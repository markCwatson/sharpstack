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

        Console.WriteLine($"Sending ARP reply to {parsed.SenderIpAddress} from {parsed.TargetIpAddress}");
        return [ArpFrameEncoder.Encode(parsed, MacAddress, Ipv4Address)];
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

        Console.WriteLine($"Sending ICMP echo reply to {packet.Source} from {packet.Destination}");
        return [IcmpFrameEncoder.Encode(incoming, packet, sourceMac, destinationMac)];
    }
}
