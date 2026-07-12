using App.Utils;

namespace App.Tests;

public class ChecksumTests
{
    [Fact]
    public void Calculate_IcmpEchoRequest_ReturnsExpectedInternetChecksum()
    {
        byte[] message =
        {
            0x08, 0x00, 0x00, 0x00,
            0x12, 0x34, 0x00, 0x01,
            0x70, 0x69, 0x6E, 0x67
        };

        ushort checksum = Checksum.Calculate(message);

        Assert.Equal((ushort)0x06FA, checksum);

        message[2] = (byte)(checksum >> 8);
        message[3] = (byte)checksum;

        Assert.Equal((ushort)0x0000, Checksum.Calculate(message));
    }
}