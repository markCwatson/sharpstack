using App.Network.Tcp;
using App.Network;
using App.Network.Ethernet;
using App.Network.IPv4;

namespace App.Tests;

public class TcpPacketTests
{
    [Fact]
    public void Parse_ReadsHeaderFieldsAndPayloadInNetworkByteOrder()
    {
        var bytes = new byte[]
        {
            // Source port: 49152
            0xC0, 0x00,

            // Destination port: 80
            0x00, 0x50,

            // Sequence number: 0x12345678
            0x12, 0x34, 0x56, 0x78,

            // Acknowledgment number: 0x9ABCDEF0
            0x9A, 0xBC, 0xDE, 0xF0,

            // Data offset: 5 words, reserved: 0, flags: ACK + PSH
            0x50, 0x18,

            // Window size: 4096
            0x10, 0x00,

            // Checksum: 0xBEEF
            0xBE, 0xEF,

            // Urgent pointer: 0
            0x00, 0x00,

            // Payload: "GET"
            0x47, 0x45, 0x54
        };

        object parsed = TcpPacket.Parse(bytes);

        var packet = Assert.IsType<TcpPacket>(parsed);
        Assert.Equal((ushort)49152, packet.Port);
        Assert.Equal((ushort)80, packet.DestinationPort);
        Assert.Equal(0x12345678u, packet.SequenceNumber);
        Assert.Equal(0x9ABCDEF0u, packet.AcknowledgmentNumber);
        Assert.Equal((ushort)5, packet.DataOffset);
        Assert.Equal((byte)0x18, packet.Flags);
        Assert.Equal((ushort)4096, packet.WindowSize);
        Assert.Equal((ushort)0xBEEF, packet.Checksum);
        Assert.Equal((ushort)0, packet.UrgentPointer);
        Assert.Equal(new byte[] { 0x47, 0x45, 0x54 }, packet.Payload);
    }

    [Fact]
    public void Parse_WithShortHeader_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TcpPacket.Parse(new byte[19]));
    }

    [Fact]
    public void UpdateState_WithSyn_AddressesSynAckToPeer()
    {
        var connection = new TcpConnection();
        var peerMac = new MacAddress(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);
        var ipv4Packet = new IPv4Packet(
            4, 5, 0, 40, 0, 0, 0, 64, (byte)Ipv4Protocol.TCP, 0,
            new Ipv4Address("10.0.0.1"), Stack.Ipv4Address, Array.Empty<byte>());
        var syn = new TcpPacket(49152, 80, 123, 0, 5, 0x02, 0, 0, 0, Array.Empty<byte>());

        EthernetFrame? response = connection.UpdateState(ipv4Packet, syn, peerMac);

        Assert.NotNull(response);
        Assert.Equal(Stack.MacAddress, response.Value.Source);
        Assert.Equal(peerMac, response.Value.Destination);
    }

}