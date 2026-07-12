using App.Network.Ethernet;
using App.Network.IPv4;

namespace App.Tests;

public class IPv4PacketTests
{
    [Fact]
    public void Parse_ReadsHeaderFieldsAndPayload()
    {
        var bytes = new byte[]
        {
            // Version 4, header length 5 words (20 bytes)
            0x45,

            // Type of service
            0x10,

            // Total length: 20-byte header + 4-byte payload
            0x00, 0x18,

            // Identification
            0x12, 0x34,

            // Flags: 2, fragment offset: 0
            0x40, 0x00,

            // TTL and protocol: ICMP
            0x40, 0x01,

            // Header checksum
            0xAB, 0xCD,

            // Source: 10.0.0.1
            0x0A, 0x00, 0x00, 0x01,

            // Destination: 10.0.0.2
            0x0A, 0x00, 0x00, 0x02,

            // Payload
            0x70, 0x69, 0x6E, 0x67
        };

        var packet = IPv4Packet.Parse(bytes);

        Assert.Equal(4, packet.Version);
        Assert.Equal(5, packet.HeaderLength);
        Assert.Equal(0x10, packet.TypeOfService);
        Assert.Equal(24, packet.TotalLength);
        Assert.Equal(0x1234, packet.Identification);
        Assert.Equal(2, packet.Flags);
        Assert.Equal(0, packet.FragmentOffset);
        Assert.Equal(64, packet.TimeToLive);
        Assert.Equal(1, packet.Protocol);
        Assert.Equal(0xABCD, packet.HeaderChecksum);
        Assert.Equal(new Ipv4Address("10.0.0.1"), packet.Source);
        Assert.Equal(new Ipv4Address("10.0.0.2"), packet.Destination);
        Assert.Equal(new byte[] { 0x70, 0x69, 0x6E, 0x67 }, packet.Payload);
    }
}
