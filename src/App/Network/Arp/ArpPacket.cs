using App.Network.Ethernet;

namespace App.Network.Arp;

public static class ArpPacket
{
    public static EthernetFrame HandlePacket(EthernetFrame incoming)
    {
        // todo: implement ARP handler
        Console.WriteLine("ARP packet received, but ARP handler is not implemented yet.");
        return incoming;
    }
}