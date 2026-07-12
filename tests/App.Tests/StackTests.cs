using App.Device;
using App.Network;
using App.Network.Ethernet;
using App.Network.ICMP;
using App.Network.IPv4;
using App.Utils;

namespace App.Tests;

public class StackTests
{
    [Fact]
    public async Task MockDevice_ArpRequest_WritesArpReply()
    {
        var sender = new MacAddress(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);
        var requestPayload = new byte[]
        {
            // Hardware type: Ethernet
            0x00, 0x01,

            // Protocol type: IPv4
            0x08, 0x00,

            // Hardware size and protocol size
            0x06, 0x04,

            // Opcode: ARP request
            0x00, 0x01,

            // Sender MAC
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,

            // Sender IP: 10.0.0.1
            0x0A, 0x00, 0x00, 0x01,

            // Target MAC: unknown
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

            // Target IP: 10.0.0.2
            0x0A, 0x00, 0x00, 0x02
        };

        var request = new EthernetFrame(
            Destination: Stack.BroadcastAddress,
            Source: sender,
            EtherType: (ushort)EtherType.ARP,
            Payload: requestPayload);

        var device = new MockDevice(request.ToBytes());
        var stack = new Stack();

        await StackRunner.ProcessOneFrame(stack, device);

        Assert.NotNull(device.WrittenFrame);
        Assert.Equal(sender, device.WrittenFrame.Value.Destination);
        Assert.Equal(Stack.MacAddress, device.WrittenFrame.Value.Source);
        Assert.Equal((ushort)EtherType.ARP, device.WrittenFrame.Value.EtherType);
        Assert.Equal(new byte[]
        {
            // Hardware type: Ethernet
            0x00, 0x01,

            // Protocol type: IPv4
            0x08, 0x00,

            // Hardware size and protocol size
            0x06, 0x04,

            // Opcode: ARP reply
            0x00, 0x02,

            // Sender MAC and IP: stack
            0x02, 0x00, 0x00, 0x00, 0x00, 0x02,
            0x0A, 0x00, 0x00, 0x02,

            // Target MAC and IP: original sender
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,
            0x0A, 0x00, 0x00, 0x01
        }, device.WrittenFrame.Value.Payload);
    }

    [Fact]
    public async Task MockDevice_IcmpEchoRequest_WritesEchoReply()
    {
        var sender = new MacAddress(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);
        var request = new EthernetFrame(
            Destination: Stack.MacAddress,
            Source: sender,
            EtherType: (ushort)EtherType.IPv4,
            Payload: new byte[]
            {
                // IPv4 header: version, IHL, TOS, total length
                0x45, 0x00, 0x00, 0x20,

                // Identification, flags, and fragment offset
                0x00, 0x00, 0x00, 0x00,

                // TTL, protocol (ICMP), header checksum
                0x40, 0x01, 0x66, 0xDB,

                // Source IP: 10.0.0.1
                0x0A, 0x00, 0x00, 0x01,

                // Destination IP: 10.0.0.2
                0x0A, 0x00, 0x00, 0x02,

                // ICMP echo request: type, code, checksum
                0x08, 0x00, 0x06, 0xFA,

                // Identifier and sequence number
                0x12, 0x34, 0x00, 0x01,

                // Payload: "ping"
                0x70, 0x69, 0x6E, 0x67
            });

        var device = new MockDevice(request.ToBytes());
        var stack = new Stack();

        await StackRunner.ProcessOneFrame(stack, device);

        Assert.NotNull(device.WrittenFrame);
        Assert.Equal(sender, device.WrittenFrame.Value.Destination);
        Assert.Equal(Stack.MacAddress, device.WrittenFrame.Value.Source);
        Assert.Equal((ushort)EtherType.IPv4, device.WrittenFrame.Value.EtherType);

        IPv4Packet response = IPv4Packet.Parse(device.WrittenFrame.Value.Payload);

        Assert.Equal(new Ipv4Address("10.0.0.2"), response.Source);
        Assert.Equal(new Ipv4Address("10.0.0.1"), response.Destination);
        Assert.Equal((byte)Ipv4Protocol.ICMP, response.Protocol);
        Assert.Equal((ushort)0x20, response.TotalLength);
        Assert.Equal((ushort)0x0000, Checksum.Calculate(response.ToBytes().AsSpan(0, response.HeaderLength * 4).ToArray()));

        IcmpPacket icmpResponse = IcmpPacket.Parse(response.Payload);

        Assert.Equal((byte)IcmpType.EchoReply, icmpResponse.Type);
        Assert.Equal((byte)IcmpCode.EchoReply, icmpResponse.Code);
        Assert.Equal((ushort)0x1234, icmpResponse.Identifier);
        Assert.Equal((ushort)0x0001, icmpResponse.SequenceNumber);
        Assert.Equal(new byte[] { 0x70, 0x69, 0x6E, 0x67 }, icmpResponse.Payload);
        Assert.Equal((ushort)0x0000, Checksum.Calculate(response.Payload));
    }
}
