using App.Network.Ethernet;
using App.Network.Arp;
using App.Network.IPv4;

namespace App.Network;

// the networking stack has to read the EthernetFrame.EtherType
// then pass it to either the ARP handler or the IPv4 handler

public sealed class Stack
{
    // this is the mac address of this stack
    private readonly MacAddress _macAddress = new MacAddress(0x02, 0x00, 0x00, 0x00, 0x00, 0x02);

    public async Task<EthernetFrame> HandleFrameAsync(EthernetFrame incoming)
    {
        return incoming.EtherTypeEnum switch
        {
            EtherType.ARP => ArpPacket.HandlePacket(incoming),
            EtherType.IPv4 => await IPv4Packet.HandlePacket(incoming),
            _ => HandleUnknown(incoming)
        };
    }

    private EthernetFrame HandleUnknown(EthernetFrame incoming)
    {
        // todo: implement unknown frame handler
        return incoming;
    }
}
