using System.Buffers.Binary;
using App.Network;
using App.Network.Ethernet;
using App.Network.IPv4;
using App.Network.Tcp;
using App.Utils;

namespace App.Tests;

public class TcpConnectionTests
{
    [Fact]
    public void ReceiveData_AccumulatesBytesUntilConsumed()
    {
        var connection = new TcpConnection();

        connection.ReceiveData([0x47, 0x45]);
        connection.ReceiveData([0x54, 0x20]);
        connection.ConsumeData(3);

        Assert.Equal(new byte[] { 0x20 }, connection.GetReceivedData());
    }

    [Fact]
    public void GetReceivedData_ReturnsCopyOfBufferedData()
    {
        var connection = new TcpConnection();
        connection.ReceiveData([0x47]);

        byte[] receivedData = connection.GetReceivedData();
        receivedData[0] = 0x58;

        Assert.Equal(new byte[] { 0x47 }, connection.GetReceivedData());
    }

    [Fact]
    public void ConsumeData_MoreThanBufferedBytes_ThrowsArgumentException()
    {
        var connection = new TcpConnection();
        connection.ReceiveData([0x47]);

        Assert.Throws<ArgumentException>(() => connection.ConsumeData(2));
    }

    [Fact]
    public void CreateEthernetFrame_ReturnsReversedSynAckWithValidChecksums()
    {
        var connection = new TcpConnection();
        var peerMac = new MacAddress(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);
        var peerIp = new Ipv4Address("10.0.0.1");
        var ipv4Packet = new IPv4Packet(
            4, 5, 0, 40, 0, 0, 0, 64, (byte)Ipv4Protocol.TCP, 0,
            peerIp, Stack.Ipv4Address, Array.Empty<byte>());
        var syn = new TcpPacket(49152, 80, 123, 0, 5, 0x02, 0, 0, 0, Array.Empty<byte>());

        EthernetFrame response = connection.CreateEthernetFrame(
            ipv4Packet, syn, Stack.MacAddress, peerMac);
        IPv4Packet responseIp = IPv4Packet.Parse(response.Payload);
        TcpPacket responseTcp = TcpPacket.Parse(responseIp.Payload);

        Assert.Equal(peerMac, response.Destination);
        Assert.Equal(Stack.MacAddress, response.Source);
        Assert.Equal((ushort)EtherType.IPv4, response.EtherType);
        Assert.Equal(Stack.Ipv4Address, responseIp.Source);
        Assert.Equal(peerIp, responseIp.Destination);
        Assert.Equal((ushort)80, responseTcp.Port);
        Assert.Equal((ushort)49152, responseTcp.DestinationPort);
        Assert.Equal(10_000u, responseTcp.SequenceNumber);
        Assert.Equal(0u, responseTcp.AcknowledgmentNumber);
        Assert.Equal((byte)0x12, responseTcp.Flags);

        byte[] responseIpBytes = responseIp.ToBytes();
        Assert.Equal((ushort)0, Checksum.Calculate(responseIpBytes[..(responseIp.HeaderLength * 4)]));

        byte[] pseudoHeader = new byte[12];
        responseIp.Source.ToArray().CopyTo(pseudoHeader, 0);
        responseIp.Destination.ToArray().CopyTo(pseudoHeader, 4);
        pseudoHeader[9] = (byte)Ipv4Protocol.TCP;
        BinaryPrimitives.WriteUInt16BigEndian(
            pseudoHeader.AsSpan(10, 2), (ushort)responseTcp.ToBytes().Length);

        Assert.Equal(
            (ushort)0,
            Checksum.Calculate(pseudoHeader.Concat(responseTcp.ToBytes()).ToArray()));
    }
}