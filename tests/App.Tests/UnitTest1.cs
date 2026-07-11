using App.Network.Ethernet;

namespace App.Tests;

public class UnitTest1
{
    [Fact]
    public void EthernetFrame_Parse_ReadsNetworkOrderIpv4EtherType()
    {
        var bytes = new byte[]
        {
            // Destination MAC: 02:00:00:00:00:02
            0x02, 0x00, 0x00, 0x00, 0x00, 0x02,

            // Source MAC: AA:BB:CC:DD:EE:FF
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,

            // EtherType IPv4: 0x0800 in network byte order
            0x08, 0x00,

            // Payload
            0x48, 0x69
        };

        var frame = EthernetFrame.Parse(bytes);

        Assert.Equal(
            new MacAddress(0x02, 0x00, 0x00, 0x00, 0x00, 0x02),
            frame.Destination);

        Assert.Equal(
            new MacAddress(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF),
            frame.Source);

        Assert.Equal(EtherType.IPv4, frame.EtherTypeEnum);
        Assert.Equal(new byte[] { 0x48, 0x69 }, frame.Payload);
    }

    [Fact]
    public void EthernetFrame_ToBytes_WritesNetworkFrameLayout()
    {
        var frame = new EthernetFrame(
            Destination: new MacAddress(0x02, 0x00, 0x00, 0x00, 0x00, 0x02),
            Source: new MacAddress(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF),
            EtherType: (ushort)EtherType.IPv4,
            Payload: new byte[] { 0x48, 0x69 });

        var bytes = frame.ToBytes();

        var expected = new byte[]
        {
            // Destination MAC
            0x02, 0x00, 0x00, 0x00, 0x00, 0x02,

            // Source MAC
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,

            // EtherType IPv4: 0x0800 in network byte order
            0x08, 0x00,

            // Payload
            0x48, 0x69
        };

        Assert.Equal(expected, bytes);
    }
}