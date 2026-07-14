using App.Application;
using App.Network.Ethernet;
using App.Network.IPv4;

namespace App.Network.Tcp;

public sealed class TcpServer
{
    private Dictionary<ushort, IApplication> _listeners = new();
    private Dictionary<(Ipv4Address, Ipv4Address, ushort, ushort), TcpConnection> _tcpConnections = new();

    public void RegisterTcpListener(ushort port, IApplication application)
    {
        _listeners[port] = application;
    }

    public async Task<EthernetFrame?> HandlePacket(IPv4Packet ipv4Packet, MacAddress sourceMac, MacAddress destinationMac)
    {
        Console.WriteLine($"Received IPv4 packet from {ipv4Packet.Source} to {ipv4Packet.Destination} with protocol {ipv4Packet.Protocol}");
        TcpPacket tcpPacket = TcpPacket.Parse(ipv4Packet.Payload);

        if (!_listeners.TryGetValue(tcpPacket.DestinationPort, out IApplication? listener) || listener is null)
            return null;

        TcpConnection conn = GetOrCreateTcpConnection(ipv4Packet.Source, ipv4Packet.Destination, tcpPacket.Port, tcpPacket.DestinationPort);
        EthernetFrame? res = conn.UpdateState(ipv4Packet, tcpPacket, sourceMac);
        if (res is not null)
            return res;

        if (!conn.IsEstablished || tcpPacket.Payload.Length == 0)
            return null;

        conn.ReceiveData(tcpPacket.Payload);

        byte[] response = await listener.HandleRequestAsync(conn);

        // return ethernet frame
        return null;
    }

    public TcpConnection GetOrCreateTcpConnection(Ipv4Address sourceIp, Ipv4Address destinationIp, ushort sourcePort, ushort destinationPort)
    {
        var key = (sourceIp, destinationIp, sourcePort, destinationPort);
        if (!_tcpConnections.TryGetValue(key, out var connection))
        {
            connection = new TcpConnection();
            _tcpConnections[key] = connection;
        }
        return connection;
    }
}
