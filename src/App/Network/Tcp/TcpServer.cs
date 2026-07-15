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
        Console.WriteLine($"TCP packet {tcpPacket.Port} -> {tcpPacket.DestinationPort}, flags=0x{tcpPacket.Flags:X2}, seq={tcpPacket.SequenceNumber}, ack={tcpPacket.AcknowledgmentNumber}, payload={tcpPacket.Payload.Length} bytes");

        if (!_listeners.TryGetValue(tcpPacket.DestinationPort, out IApplication? listener) || listener is null)
        {
            Console.WriteLine($"No TCP listener is registered for port {tcpPacket.DestinationPort}");
            return null;
        }

        Console.WriteLine($"TCP listener found for port {tcpPacket.DestinationPort}");

        TcpConnection conn = GetOrCreateTcpConnection(ipv4Packet.Source, ipv4Packet.Destination, tcpPacket.Port, tcpPacket.DestinationPort);
        bool isNewPayload = tcpPacket.Payload.Length > 0 && tcpPacket.SequenceNumber == conn.NextExpectedSequenceNumber;
        EthernetFrame? res = conn.UpdateState(ipv4Packet, tcpPacket, sourceMac);
        Console.WriteLine($"TCP connection state after packet: established={conn.IsEstablished}, response={(res is not null ? "generated" : "none")}");
        if (res is not null)
            return res;

        if (!conn.IsEstablished)
        {
            Console.WriteLine("TCP payload was not dispatched because the connection is not established");
            return null;
        }

        if (tcpPacket.Payload.Length == 0)
        {
            Console.WriteLine("TCP connection is established, but this packet has no payload");
            return null;
        }

        if (!isNewPayload)
        {
            Console.WriteLine("TCP payload is a retransmission or out-of-order segment; sending acknowledgment without dispatching it");
            return conn.CreateAcknowledgmentFrame(ipv4Packet, tcpPacket, Stack.MacAddress, sourceMac);
        }

        conn.ReceiveData(tcpPacket.Payload);
        Console.WriteLine($"Dispatching {tcpPacket.Payload.Length} payload bytes to the application");

        byte[] response = await listener.HandleRequestAsync(conn);
        Console.WriteLine($"Application returned {response.Length} response bytes");

        return response.Length == 0
            ? conn.CreateAcknowledgmentFrame(ipv4Packet, tcpPacket, Stack.MacAddress, sourceMac)
            : conn.CreateResponseFrame(ipv4Packet, tcpPacket, Stack.MacAddress, sourceMac, response);
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
