using App.Application;
using App.Network;
using App.Network.Ethernet;
using App.Network.IPv4;
using App.Network.Tcp;

namespace App.Tests;

public class TcpServerTests
{
    [Fact]
    public void GetOrCreateTcpConnection_ReusesConnectionForSameFourTuple()
    {
        var server = new TcpServer();
        var remoteIp = new Ipv4Address("10.0.0.1");

        TcpConnection first = server.GetOrCreateTcpConnection((remoteIp, Stack.Ipv4Address, 49152, 80));
        TcpConnection second = server.GetOrCreateTcpConnection((remoteIp, Stack.Ipv4Address, 49152, 80));
        TcpConnection otherPort = server.GetOrCreateTcpConnection((remoteIp, Stack.Ipv4Address, 49153, 80));

        Assert.Same(first, second);
        Assert.NotSame(first, otherPort);
    }

    [Fact]
    public async Task HandlePacket_ForUnregisteredPort_ReturnsNoFrames()
    {
        var server = new TcpServer();

        IReadOnlyList<EthernetFrame> response = await server.HandlePacket(
            CreateIpv4Packet(Packet(sequence: 123, flags: TcpFlag.SYN)),
            PeerMac,
            Stack.MacAddress);

        Assert.Empty(response);
    }

    [Fact]
    public async Task HandlePacket_AccumulatesAcceptedPayloadBeforeDispatchingToListener()
    {
        var application = new RecordingApplication();
        TcpServer server = EstablishedServer(application);

        IReadOnlyList<EthernetFrame> firstFrames = await server.HandlePacket(
            CreateIpv4Packet(Packet(124, 10_001, TcpFlag.PSH | TcpFlag.ACK, [0x47, 0x45])),
            PeerMac,
            Stack.MacAddress);
        IReadOnlyList<EthernetFrame> secondFrames = await server.HandlePacket(
            CreateIpv4Packet(Packet(126, 10_001, TcpFlag.PSH | TcpFlag.ACK, [0x54])),
            PeerMac,
            Stack.MacAddress);

        Assert.Single(firstFrames);
        Assert.Single(secondFrames);
        Assert.Equal(2, application.Requests.Count);
        Assert.Equal(new byte[] { 0x47, 0x45 }, application.Requests[0]);
        Assert.Equal(new byte[] { 0x47, 0x45, 0x54 }, application.Requests[1]);
    }

    [Fact]
    public async Task HandlePacket_AcknowledgesRetransmissionWithoutDispatchingItAgain()
    {
        var application = new RecordingApplication();
        TcpServer server = EstablishedServer(application);
        TcpPacket request = Packet(124, 10_001, TcpFlag.PSH | TcpFlag.ACK, [0x47]);

        IReadOnlyList<EthernetFrame> firstFrames = await server.HandlePacket(
            CreateIpv4Packet(request), PeerMac, Stack.MacAddress);
        IReadOnlyList<EthernetFrame> retransmissionFrames = await server.HandlePacket(
            CreateIpv4Packet(request), PeerMac, Stack.MacAddress);

        Assert.Single(application.Requests);
        Assert.Equal(new byte[] { 0x47 }, application.Requests[0]);
        Assert.Equal(125u, ParseTcp(Assert.Single(firstFrames)).AcknowledgmentNumber);
        Assert.Equal(125u, ParseTcp(Assert.Single(retransmissionFrames)).AcknowledgmentNumber);
    }

    [Fact]
    public async Task HandlePacket_OrdersReceiveSendAndCloseOutputs()
    {
        var application = new RecordingApplication(new ApplicationResult([0x41, 0x42], CloseConnection: true));
        TcpServer server = EstablishedServer(application);

        IReadOnlyList<EthernetFrame> frames = await server.HandlePacket(
            CreateIpv4Packet(Packet(124, 10_001, TcpFlag.PSH | TcpFlag.ACK, [0x47])),
            PeerMac,
            Stack.MacAddress);

        Assert.Equal(3, frames.Count);
        TcpPacket acknowledgment = ParseTcp(frames[0]);
        TcpPacket response = ParseTcp(frames[1]);
        TcpPacket fin = ParseTcp(frames[2]);
        Assert.Equal((byte)TcpFlag.ACK, acknowledgment.Flags);
        Assert.Equal((byte)(TcpFlag.PSH | TcpFlag.ACK), response.Flags);
        Assert.Equal(new byte[] { 0x41, 0x42 }, response.Payload);
        Assert.Equal(10_001u, response.SequenceNumber);
        Assert.Equal((byte)(TcpFlag.FIN | TcpFlag.ACK), fin.Flags);
        Assert.Equal(10_003u, fin.SequenceNumber);
    }

    [Fact]
    public async Task HandlePacket_PeerFinAcknowledgesWithoutAutomaticallyClosing()
    {
        var application = new RecordingApplication();
        TcpServer server = EstablishedServer(application);

        IReadOnlyList<EthernetFrame> frames = await server.HandlePacket(
            CreateIpv4Packet(Packet(124, 10_001, TcpFlag.FIN | TcpFlag.ACK)),
            PeerMac,
            Stack.MacAddress);
        TcpConnection connection = server.GetOrCreateTcpConnection(
            (PeerIp, Stack.Ipv4Address, 49152, 80));

        Assert.Equal((byte)TcpFlag.ACK, ParseTcp(Assert.Single(frames)).Flags);
        Assert.Equal(TcpState.CloseWait, connection.State);
        Assert.Empty(application.Requests);
    }

    [Fact]
    public async Task GetOrCreateTcpConnection_ReplacesConnectionAfterTimeWaitExpires()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var application = new RecordingApplication(new ApplicationResult([0x41], CloseConnection: true));
        var server = new TcpServer(() => now, TimeSpan.FromSeconds(30));
        server.RegisterTcpListener(80, application);
        await Establish(server);
        TcpConnection original = server.GetOrCreateTcpConnection((PeerIp, Stack.Ipv4Address, 49152, 80));

        await server.HandlePacket(
            CreateIpv4Packet(Packet(124, 10_001, TcpFlag.PSH | TcpFlag.ACK, [0x47])),
            PeerMac,
            Stack.MacAddress);
        await server.HandlePacket(
            CreateIpv4Packet(Packet(125, 10_003, TcpFlag.ACK)),
            PeerMac,
            Stack.MacAddress);
        await server.HandlePacket(
            CreateIpv4Packet(Packet(125, 10_003, TcpFlag.FIN | TcpFlag.ACK)),
            PeerMac,
            Stack.MacAddress);

        Assert.Equal(TcpState.TimeWait, original.State);
        Assert.Same(original, server.GetOrCreateTcpConnection((PeerIp, Stack.Ipv4Address, 49152, 80)));

        now += TimeSpan.FromSeconds(30);
        TcpConnection replacement = server.GetOrCreateTcpConnection((PeerIp, Stack.Ipv4Address, 49152, 80));
        Assert.NotSame(original, replacement);
        Assert.Equal(TcpState.Listen, replacement.State);
    }

    private static TcpServer EstablishedServer(RecordingApplication application)
    {
        var server = new TcpServer();
        server.RegisterTcpListener(80, application);
        Establish(server).GetAwaiter().GetResult();
        return server;
    }

    private static async Task Establish(TcpServer server)
    {
        await server.HandlePacket(
            CreateIpv4Packet(Packet(123, flags: TcpFlag.SYN)),
            PeerMac,
            Stack.MacAddress);
        await server.HandlePacket(
            CreateIpv4Packet(Packet(124, 10_001, TcpFlag.ACK)),
            PeerMac,
            Stack.MacAddress);
    }

    private static TcpPacket ParseTcp(EthernetFrame frame) =>
        TcpPacket.Parse(IPv4Packet.Parse(frame.Payload).Payload);

    private static TcpPacket Packet(
        uint sequence,
        uint acknowledgment = 0,
        TcpFlag flags = 0,
        byte[]? payload = null) =>
        new(49152, 80, sequence, acknowledgment, 5, (byte)flags, ushort.MaxValue, 0, 0, payload ?? []);

    private static IPv4Packet CreateIpv4Packet(TcpPacket tcpPacket)
    {
        byte[] payload = tcpPacket.ToBytes();
        return new IPv4Packet(
            4, 5, 0, (ushort)(20 + payload.Length), 0, 0, 0, 64, (byte)Ipv4Protocol.TCP, 0,
            PeerIp, Stack.Ipv4Address, payload);
    }

    private static Ipv4Address PeerIp { get; } = new("10.0.0.1");
    private static MacAddress PeerMac { get; } = new(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);

    private sealed class RecordingApplication : IApplication
    {
        private readonly ApplicationResult _result;

        public RecordingApplication(ApplicationResult? result = null)
        {
            _result = result ?? ApplicationResult.Empty;
        }

        public List<byte[]> Requests { get; } = [];

        public Task<ApplicationResult> HandleRequestAsync(TcpConnection connection)
        {
            Requests.Add(connection.GetReceivedData());
            return Task.FromResult(_result);
        }
    }
}
