using System.Buffers.Binary;
using App.Network;
using App.Network.Ethernet;
using App.Network.IPv4;
using App.Network.Tcp;
using App.Utils;

namespace App.Tests;

public class TcpFrameEncoderTests
{
    [Fact]
    public void Encode_PreservesTcpPacketAndProducesValidChecksums()
    {
        var peerMac = new MacAddress(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);
        var peerIp = new Ipv4Address("10.0.0.1");
        var incomingIp = new IPv4Packet(
            4, 5, 0, 40, 7, 0, 0, 64, (byte)Ipv4Protocol.TCP, 0,
            peerIp, Stack.Ipv4Address, []);
        var outbound = new TcpPacket(
            80, 49152, 10_000, 124, 5,
            (byte)(TcpFlag.SYN | TcpFlag.ACK), ushort.MaxValue, 0, 0, []);

        EthernetFrame frame = TcpFrameEncoder.Encode(outbound, incomingIp, peerMac);
        IPv4Packet encodedIp = IPv4Packet.Parse(frame.Payload);
        TcpPacket encodedTcp = TcpPacket.Parse(encodedIp.Payload);

        Assert.Equal(peerMac, frame.Destination);
        Assert.Equal(Stack.MacAddress, frame.Source);
        Assert.Equal((ushort)EtherType.IPv4, frame.EtherType);
        Assert.Equal(Stack.Ipv4Address, encodedIp.Source);
        Assert.Equal(peerIp, encodedIp.Destination);
        Assert.Equal(outbound with { Checksum = encodedTcp.Checksum }, encodedTcp);

        byte[] encodedIpBytes = encodedIp.ToBytes();
        Assert.Equal((ushort)0, Checksum.Calculate(encodedIpBytes[..(encodedIp.HeaderLength * 4)]));

        byte[] pseudoHeader = new byte[12];
        encodedIp.Source.ToArray().CopyTo(pseudoHeader, 0);
        encodedIp.Destination.ToArray().CopyTo(pseudoHeader, 4);
        pseudoHeader[9] = (byte)Ipv4Protocol.TCP;
        BinaryPrimitives.WriteUInt16BigEndian(
            pseudoHeader.AsSpan(10, 2),
            (ushort)encodedTcp.ToBytes().Length);

        Assert.Equal(
            (ushort)0,
            Checksum.Calculate(pseudoHeader.Concat(encodedTcp.ToBytes()).ToArray()));
    }
}
