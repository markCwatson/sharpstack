using App.Network.Tcp;

namespace App.Tests;

public class TcpConnectionTests
{
    [Fact]
    public void Receive_WithSyn_EmitsSynAckAndEstablishesAfterValidAck()
    {
        var connection = new TcpConnection(80, 49152);

        TcpResult synResult = connection.Receive(Packet(sequence: 123, flags: TcpFlag.SYN | TcpFlag.ECE | TcpFlag.CWR));

        TcpPacket synAck = Assert.Single(synResult.Outbound);
        Assert.Equal((ushort)80, synAck.Port);
        Assert.Equal((ushort)49152, synAck.DestinationPort);
        Assert.Equal(10_000u, synAck.SequenceNumber);
        Assert.Equal(124u, synAck.AcknowledgmentNumber);
        Assert.Equal((byte)(TcpFlag.SYN | TcpFlag.ACK), synAck.Flags);
        Assert.Equal(TcpState.SynReceived, connection.State);

        TcpResult ackResult = connection.Receive(Packet(sequence: 124, acknowledgment: 10_001, flags: TcpFlag.ACK));

        Assert.Empty(ackResult.Outbound);
        Assert.Equal(TcpState.Established, connection.State);
    }

    [Fact]
    public void Receive_InOrderPayload_BuffersDataAndAcknowledgesIt()
    {
        TcpConnection connection = EstablishedConnection();

        TcpResult first = connection.Receive(Packet(sequence: 124, acknowledgment: 10_001, flags: TcpFlag.PSH | TcpFlag.ACK, payload: [0x47, 0x45]));
        TcpResult second = connection.Receive(Packet(sequence: 126, acknowledgment: 10_001, flags: TcpFlag.PSH | TcpFlag.ACK, payload: [0x54, 0x20]));
        connection.ConsumeData(3);

        Assert.True(first.DataAvailable);
        Assert.True(second.DataAvailable);
        Assert.Equal(126u, Assert.Single(first.Outbound).AcknowledgmentNumber);
        Assert.Equal(128u, Assert.Single(second.Outbound).AcknowledgmentNumber);
        Assert.Equal(new byte[] { 0x20 }, connection.GetReceivedData());
    }

    [Fact]
    public void Receive_RetransmittedPayload_AcknowledgesWithoutDispatchingDataAgain()
    {
        TcpConnection connection = EstablishedConnection();
        TcpPacket payload = Packet(sequence: 124, acknowledgment: 10_001, flags: TcpFlag.PSH | TcpFlag.ACK, payload: [0x47]);

        TcpResult first = connection.Receive(payload);
        TcpResult retransmission = connection.Receive(payload);

        Assert.True(first.DataAvailable);
        Assert.False(retransmission.DataAvailable);
        Assert.Equal(125u, Assert.Single(retransmission.Outbound).AcknowledgmentNumber);
        Assert.Equal(new byte[] { 0x47 }, connection.GetReceivedData());
    }

    [Fact]
    public void GetReceivedData_ReturnsCopyAndConsumeRejectsInvalidCount()
    {
        TcpConnection connection = EstablishedConnection();
        connection.Receive(Packet(sequence: 124, acknowledgment: 10_001, flags: TcpFlag.ACK, payload: [0x47]));

        byte[] received = connection.GetReceivedData();
        received[0] = 0x58;

        Assert.Equal(new byte[] { 0x47 }, connection.GetReceivedData());
        Assert.Throws<ArgumentOutOfRangeException>(() => connection.ConsumeData(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => connection.ConsumeData(-1));
    }

    [Fact]
    public void SendAndClose_ConsumePayloadAndFinSequenceSpace()
    {
        TcpConnection connection = EstablishedConnection();

        TcpPacket data = Assert.Single(connection.Send([0x41, 0x42]).Outbound);
        TcpPacket fin = Assert.Single(connection.Close().Outbound);

        Assert.Equal(10_001u, data.SequenceNumber);
        Assert.Equal((byte)(TcpFlag.PSH | TcpFlag.ACK), data.Flags);
        Assert.Equal(10_003u, fin.SequenceNumber);
        Assert.Equal((byte)(TcpFlag.FIN | TcpFlag.ACK), fin.Flags);
        Assert.Equal(TcpState.FinWait1, connection.State);
    }

    [Fact]
    public void PeerClose_WaitsForApplicationCloseAndFinalAck()
    {
        TcpConnection connection = EstablishedConnection();

        TcpResult peerFin = connection.Receive(Packet(sequence: 124, acknowledgment: 10_001, flags: TcpFlag.FIN | TcpFlag.ACK));

        Assert.True(peerFin.PeerClosed);
        Assert.Equal((byte)TcpFlag.ACK, Assert.Single(peerFin.Outbound).Flags);
        Assert.Equal(TcpState.CloseWait, connection.State);

        TcpPacket localFin = Assert.Single(connection.Close().Outbound);
        Assert.Equal(10_001u, localFin.SequenceNumber);
        Assert.Equal(125u, localFin.AcknowledgmentNumber);
        Assert.Equal(TcpState.LastAck, connection.State);

        connection.Receive(Packet(sequence: 125, acknowledgment: 10_002, flags: TcpFlag.ACK));
        Assert.Equal(TcpState.Closed, connection.State);
    }

    [Fact]
    public void LocalClose_ExpiresAfterTimeWait()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var connection = new TcpConnection(80, 49152, getCurrentTime: () => now, timeWaitDuration: TimeSpan.FromSeconds(30));
        Establish(connection);

        connection.Close();
        connection.Receive(Packet(sequence: 124, acknowledgment: 10_002, flags: TcpFlag.ACK));
        TcpResult peerFin = connection.Receive(Packet(sequence: 124, acknowledgment: 10_002, flags: TcpFlag.FIN | TcpFlag.ACK));

        Assert.Equal(TcpState.TimeWait, connection.State);
        Assert.Equal(125u, Assert.Single(peerFin.Outbound).AcknowledgmentNumber);
        Assert.False(connection.TryExpireTimeWait());

        now += TimeSpan.FromSeconds(30);
        Assert.True(connection.TryExpireTimeWait());
        Assert.Equal(TcpState.Closed, connection.State);
    }

    [Fact]
    public void SimultaneousClose_TransitionsThroughClosingToTimeWait()
    {
        TcpConnection connection = EstablishedConnection();
        connection.Close();

        connection.Receive(Packet(sequence: 124, acknowledgment: 10_001, flags: TcpFlag.FIN | TcpFlag.ACK));
        Assert.Equal(TcpState.Closing, connection.State);

        connection.Receive(Packet(sequence: 125, acknowledgment: 10_002, flags: TcpFlag.ACK));
        Assert.Equal(TcpState.TimeWait, connection.State);
    }

    private static TcpConnection EstablishedConnection()
    {
        var connection = new TcpConnection(80, 49152);
        Establish(connection);
        return connection;
    }

    private static void Establish(TcpConnection connection)
    {
        connection.Receive(Packet(sequence: 123, flags: TcpFlag.SYN));
        connection.Receive(Packet(sequence: 124, acknowledgment: 10_001, flags: TcpFlag.ACK));
    }

    private static TcpPacket Packet(
        uint sequence,
        uint acknowledgment = 0,
        TcpFlag flags = 0,
        byte[]? payload = null) =>
        new(49152, 80, sequence, acknowledgment, 5, (byte)flags, ushort.MaxValue, 0, 0, payload ?? []);
}
