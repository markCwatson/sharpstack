using ScratchHttpServer.Network.Ethernet;

namespace ScratchHttpServer.Network.IPv4;

public static class IPv4Packet
{
    public static async Task<EthernetFrame> HandlePacket(EthernetFrame incoming)
    {
        // todo: implement IPv4 handler
        return incoming;
    }
}
