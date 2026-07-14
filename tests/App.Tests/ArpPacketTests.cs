using App.Network;
using App.Network.Arp;
using App.Network.Ethernet;

namespace App.Tests;

public class ArpPacketTests
{
    [Fact]
    public void ToBytes_WritesArpFieldsInNetworkByteOrder()
    {
        var packet = new ArpPacket(
            HardwareType: 0x0001,
            ProtocolType: 0x0800,
            HardwareSize: 6,
            ProtocolSize: 4,
            Opcode: 0x0001,
            SenderMacAddress: new MacAddress(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF),
            SenderIpAddress: new Ipv4Address("10.0.0.1"),
            TargetMacAddress: new MacAddress(0x02, 0x00, 0x00, 0x00, 0x00, 0x02),
            TargetIpAddress: new Ipv4Address("10.0.0.2"));

        var bytes = packet.ToBytes();

        Assert.Equal(new byte[]
        {
            0x00, 0x01,
            0x08, 0x00,
            0x06,
            0x04,
            0x00, 0x01,
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,
            0x0A, 0x00, 0x00, 0x01,
            0x02, 0x00, 0x00, 0x00, 0x00, 0x02,
            0x0A, 0x00, 0x00, 0x02
        }, bytes);
    }

    [Fact]
    public void Parse_ReadsArpFieldsInNetworkByteOrder()
    {
        var payload = new byte[]
        {
            0x00, 0x01,
            0x08, 0x00,
            0x06,
            0x04,
            0x00, 0x01,
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,
            0x0A, 0x00, 0x00, 0x01,
            0x02, 0x00, 0x00, 0x00, 0x00, 0x02,
            0x0A, 0x00, 0x00, 0x02
        };

        var packet = ArpPacket.Parse(payload);

        Assert.Equal(0x0001, packet.HardwareType);
        Assert.Equal(0x0800, packet.ProtocolType);
        Assert.Equal(6, packet.HardwareSize);
        Assert.Equal(4, packet.ProtocolSize);
        Assert.Equal(1, packet.Opcode);
        Assert.Equal(
            new MacAddress(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF),
            packet.SenderMacAddress);
        Assert.Equal(new Ipv4Address("10.0.0.1"), packet.SenderIpAddress);
        Assert.Equal(
            new MacAddress(0x02, 0x00, 0x00, 0x00, 0x00, 0x02),
            packet.TargetMacAddress);
        Assert.Equal(new Ipv4Address("10.0.0.2"), packet.TargetIpAddress);
    }

    [Fact]
    public void StackHandleArpPacket_ArpRequestWithUnknownTargetMac_ReturnsReply()
    {
        var sender = new MacAddress(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);
        var requestPayload = new byte[]
        {
            0x00, 0x01,
            0x08, 0x00,
            0x06,
            0x04,
            0x00, 0x01,
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,
            0x0A, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x0A, 0x00, 0x00, 0x02
        };
        var incoming = new EthernetFrame(
            Destination: Stack.BroadcastAddress,
            Source: sender,
            EtherType: (ushort)EtherType.ARP,
            Payload: requestPayload);

        var response = Stack.HandleArpPacket(incoming);

        Assert.NotNull(response);
        Assert.Equal(sender, response.Value.Destination);
        Assert.Equal(Stack.MacAddress, response.Value.Source);
        Assert.Equal(new byte[]
        {
            0x00, 0x01,
            0x08, 0x00,
            0x06,
            0x04,
            0x00, 0x02,
            0x02, 0x00, 0x00, 0x00, 0x00, 0x02,
            0x0A, 0x00, 0x00, 0x02,
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,
            0x0A, 0x00, 0x00, 0x01
        }, response.Value.Payload);
    }

    [Fact]
    public void StackHandleArpPacket_ArpReply_DoesNotGenerateAnotherReply()
    {
        var replyPayload = new byte[]
        {
            0x00, 0x01,
            0x08, 0x00,
            0x06,
            0x04,
            0x00, 0x02,
            0x02, 0x00, 0x00, 0x00, 0x00, 0x02,
            0x0A, 0x00, 0x00, 0x02,
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,
            0x0A, 0x00, 0x00, 0x01
        };
        var incoming = new EthernetFrame(
            Destination: new MacAddress(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF),
            Source: Stack.MacAddress,
            EtherType: (ushort)EtherType.ARP,
            Payload: replyPayload);

        var response = Stack.HandleArpPacket(incoming);

        Assert.Null(response);
    }
}