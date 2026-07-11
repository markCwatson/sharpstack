using App.Network.Ethernet;

namespace App.Tests;

public class IpAddressTests
{
    [Fact]
    public void ToArray_ConvertsDottedAddressToFourBytes()
    {
        var address = new Ipv4Address("10.0.0.2");

        var bytes = address.ToArray();

        Assert.Equal(new byte[] { 0x0A, 0x00, 0x00, 0x02 }, bytes);
    }

    [Fact]
    public void ToArray_PreservesEveryOctet()
    {
        var address = new Ipv4Address("192.168.1.254");

        var bytes = address.ToArray();

        Assert.Equal(new byte[] { 0xC0, 0xA8, 0x01, 0xFE }, bytes);
    }

    [Fact]
    public void ToArray_WithWrongNumberOfOctets_ThrowsFormatException()
    {
        var address = new Ipv4Address("10.0.2");

        Assert.Throws<FormatException>(() => address.ToArray());
    }

    [Fact]
    public void ToArray_WithNonNumericOctet_ThrowsFormatException()
    {
        var address = new Ipv4Address("10.x.0.2");

        Assert.Throws<FormatException>(() => address.ToArray());
    }

    [Fact]
    public void ToArray_WithOutOfRangeOctet_ThrowsOverflowException()
    {
        var address = new Ipv4Address("10.256.0.2");

        Assert.Throws<OverflowException>(() => address.ToArray());
    }
}
