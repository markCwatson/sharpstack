using App.Network.ICMP;

namespace App.Tests;

public class IcmpPacketTests
{
    [Fact]
    public void Parse_ReadsEchoRequestFieldsInNetworkByteOrder()
    {
        var bytes = new byte[]
        {
            // Type: echo request
            0x08,

            // Code
            0x00,

            // Checksum: 0x06FA
            0x06, 0xFA,

            // Identifier: 0x1234
            0x12, 0x34,

            // Sequence number: 0x0001
            0x00, 0x01,

            // Payload: "ping"
            0x70, 0x69, 0x6E, 0x67
        };

        var packet = IcmpPacket.Parse(bytes);

        Assert.Equal(8, packet.Type);
        Assert.Equal(0, packet.Code);
        Assert.Equal(0x06FA, packet.Checksum);
        Assert.Equal(0x1234, packet.Identifier);
        Assert.Equal(1, packet.SequenceNumber);
        Assert.Equal(new byte[] { 0x70, 0x69, 0x6E, 0x67 }, packet.Payload);
    }

    [Fact]
    public void Parse_PreservesEmptyEchoPayload()
    {
        var bytes = new byte[]
        {
            // Type: echo request
            0x08,

            // Code
            0x00,

            // Checksum
            0x00, 0x00,

            // Identifier
            0x00, 0x01,

            // Sequence number
            0x00, 0x02
        };

        var packet = IcmpPacket.Parse(bytes);

        Assert.Equal(8, packet.Type);
        Assert.Equal(0, packet.Code);
        Assert.Equal(1, packet.Identifier);
        Assert.Equal(2, packet.SequenceNumber);
        Assert.Empty(packet.Payload);
    }

    [Fact]
    public void Parse_WithShortPacket_ThrowsArgumentException()
    {
        var bytes = new byte[7];

        Assert.Throws<ArgumentException>(() => IcmpPacket.Parse(bytes));
    }
}
