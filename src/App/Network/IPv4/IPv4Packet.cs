using App.Network.Ethernet;

namespace App.Network.IPv4;

public sealed record IPv4Packet
{
    public static async Task<EthernetFrame> HandlePacket(EthernetFrame incoming)
    {
        // todo: implement IPv4 handler
        return incoming;
    }
}
