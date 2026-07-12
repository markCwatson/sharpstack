using App.Network.Ethernet;
using App.Network.Arp;
using App.Network.IPv4;

namespace App.Network;

// the networking stack has to read the EthernetFrame.EtherType
// then pass it to either the ARP handler or the IPv4 handler

public sealed class Stack
{
    // this is the mac address and ip of this stack
    public static MacAddress MacAddress { get; } = new MacAddress(0x02, 0x00, 0x00, 0x00, 0x00, 0x02);
    public static Ipv4Address Ipv4Address { get; } = new Ipv4Address("10.0.0.2");

    public static MacAddress BroadcastAddress { get; } = new MacAddress(0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);

    public async Task<EthernetFrame?> HandleFrameAsync(byte[] bytes)
    {
        EthernetFrame incoming = EthernetFrame.Parse(bytes);

        return incoming.EtherTypeEnum switch
        {
            EtherType.ARP => ArpPacket.HandlePacket(incoming),
            EtherType.IPv4 => await IPv4Packet.HandlePacket(incoming),
            _ => null
        };
    }
}
