using App.Network.Ethernet;
using App.Network.Arp;
using App.Network.IPv4;
using App.Application;
using App.Network.Tcp;
using App.Network.ICMP;

namespace App.Network;

// the networking stack has to read the EthernetFrame.EtherType
// then pass it to either the ARP handler or the IPv4 handler

public sealed class Stack
{
    private readonly TcpServer _tcpServer;

    public Stack()
    {
        _tcpServer = new TcpServer();
    }

    public void RegisterTcpListener(ushort port, IApplication application)
    {
        _tcpServer.RegisterTcpListener(port, application);
    }

    // this is the mac address and ip of this stack
    public static MacAddress MacAddress { get; } = new MacAddress(0x02, 0x00, 0x00, 0x00, 0x00, 0x02);
    public static Ipv4Address Ipv4Address { get; } = new Ipv4Address("10.0.0.2");
    public static MacAddress BroadcastAddress { get; } = new MacAddress(0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);

    public async Task<IReadOnlyList<EthernetFrame>> HandleEthernetFrameAsync(byte[] bytes)
    {
        EthernetFrame incoming = EthernetFrame.Parse(bytes);

        return incoming.EtherTypeEnum switch
        {
            EtherType.ARP => HandleArpPacket(incoming),
            EtherType.IPv4 => await HandleIpv4Packet(incoming),
            _ => []
        };
    }

    public static IReadOnlyList<EthernetFrame> HandleArpPacket(EthernetFrame incoming)
    {
        ArpPacket parsed = ArpPacket.Parse(incoming.Payload);

        if (parsed.Opcode != 1 || !parsed.TargetIpAddress.Equals(Stack.Ipv4Address))
            return [];

        ArpPacket response = new ArpPacket(
            HardwareType: parsed.HardwareType,
            ProtocolType: parsed.ProtocolType,
            HardwareSize: parsed.HardwareSize,
            ProtocolSize: parsed.ProtocolSize,
            Opcode: 2, // reply
            SenderMacAddress: Stack.MacAddress,
            SenderIpAddress: parsed.TargetIpAddress,
            TargetMacAddress: parsed.SenderMacAddress,
            TargetIpAddress: parsed.SenderIpAddress
        );

        return [new EthernetFrame(
            Destination: incoming.Source,
            Source: Stack.MacAddress,
            EtherType: (ushort)EtherType.ARP,
            Payload: response.ToBytes()
        )];
    }

    public async Task<IReadOnlyList<EthernetFrame>> HandleIpv4Packet(EthernetFrame incoming)
    {
        IPv4Packet packet = IPv4Packet.Parse(incoming.Payload);

        if (packet.Destination != Ipv4Address)
            return [];

        return packet.ProtocolEnum switch
        {
            Ipv4Protocol.ICMP => await HandleIcmpPacket(packet, incoming.Source, incoming.Destination),
            Ipv4Protocol.TCP => await _tcpServer.HandlePacket(packet, incoming.Source, incoming.Destination),
            _ => []
        };
    }

    public static async Task<IReadOnlyList<EthernetFrame>> HandleIcmpPacket(IPv4Packet packet, MacAddress sourceMac, MacAddress destinationMac)
    {
        IcmpPacket incoming = IcmpPacket.Parse(packet.Payload);

        if (incoming.TypeEnum != IcmpType.EchoRequest)
            return [];

        IcmpPacket reply = new IcmpPacket(
            (byte)IcmpType.EchoReply,
            (byte)IcmpCode.EchoReply,
            0, // checksum
            incoming.Identifier,
            incoming.SequenceNumber,
            incoming.Payload
        );

        byte[] replyInBytes = reply.ToBytes();
        ushort replyChecksum = Utils.Checksum.Calculate(replyInBytes);
        reply = reply with { Checksum = replyChecksum };

        IPv4Packet ipv4Wrap = new IPv4Packet(
            packet.Version,
            packet.HeaderLength,
            packet.TypeOfService,
            (ushort)(packet.HeaderLength * 4 + reply.ToBytes().Length),
            packet.Identification,
            packet.Flags,
            packet.FragmentOffset,
            packet.TimeToLive,
            packet.Protocol,
            0, // checksum
            packet.Destination,
            packet.Source,
            reply.ToBytes() // wrapped in IPv4 packet
        );

        byte[] inBytes = ipv4Wrap.ToBytes();
        ushort ipv4WrapChecksum = Utils.Checksum.Calculate(inBytes);
        ipv4Wrap = ipv4Wrap with { HeaderChecksum = ipv4WrapChecksum };

        Console.WriteLine($"Sending ICMP echo reply to {packet.Source} from {packet.Destination}");

        return [new EthernetFrame(
            sourceMac,
            MacAddress,
            (ushort)EtherType.IPv4,
            ipv4Wrap.ToBytes() // wrapped in ethernet frame
        )];
    }
}
