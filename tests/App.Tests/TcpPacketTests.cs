using App.Network.Tcp;

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

}