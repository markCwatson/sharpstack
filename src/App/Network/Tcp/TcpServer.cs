using App.Application;
using App.Network.Ethernet;
using App.Network.IPv4;

namespace App.Network.Tcp;

// connection lookup, application calls, and output collection
public sealed class TcpServer
{
    private readonly Dictionary<ushort, IApplication> _listeners = [];
    private readonly Dictionary<
        (Ipv4Address SourceIp, Ipv4Address DestinationIp, ushort SourcePort, ushort DestinationPort),
        TcpConnection> _tcpConnections = [];
    private readonly Func<DateTimeOffset>? _getCurrentTime;
    private readonly TimeSpan? _timeWaitDuration;

    public TcpServer(
        Func<DateTimeOffset>? getCurrentTime = null,
        TimeSpan? timeWaitDuration = null)
    {
        _getCurrentTime = getCurrentTime;
        _timeWaitDuration = timeWaitDuration;
    }

    public void RegisterTcpListener(ushort port, IApplication application)
    {
        _listeners[port] = application;
    }

    public async Task<IReadOnlyList<EthernetFrame>> HandlePacket(IPv4Packet ipv4Packet, MacAddress sourceMac, MacAddress destinationMac)
    {
        TcpPacket tcpPacket = TcpPacket.Parse(ipv4Packet.Payload);

        Console.WriteLine(
            $"TCP IN {ipv4Packet.Destination.Value}:{tcpPacket.DestinationPort} " +
            $"<- {ipv4Packet.Source.Value}:{tcpPacket.Port} " +
            $"flags=[{(TcpFlag)tcpPacket.Flags}] " +
            $"seq={tcpPacket.SequenceNumber} " +
            $"ack={tcpPacket.AcknowledgmentNumber} " +
            $"payload={tcpPacket.Payload.Length}B");

        if (!_listeners.TryGetValue(tcpPacket.DestinationPort, out IApplication? listener) || listener is null)
            return [];

        var key = (ipv4Packet.Source, ipv4Packet.Destination, tcpPacket.Port, tcpPacket.DestinationPort);
        TcpConnection conn = GetOrCreateTcpConnection(key);
        List<(TcpPacket Packet, TcpState State)> outbound = [];

        TcpResult receiveResult = conn.Receive(tcpPacket);
        outbound.AddRange(receiveResult.Outbound.Select(packet => (packet, conn.State)));

        if (receiveResult.DataAvailable)
        {
            ApplicationResult applicationResult = await listener.HandleRequestAsync(conn);

            if (applicationResult.Response.Length > 0)
                outbound.AddRange(conn.Send(applicationResult.Response).Outbound.Select(packet => (packet, conn.State)));

            if (applicationResult.CloseConnection)
                outbound.AddRange(conn.Close().Outbound.Select(packet => (packet, conn.State)));
        }

        if (conn.IsClosed)
            _tcpConnections.Remove(key);

        EthernetFrame[] frames = outbound
            .Select(entry => TcpFrameEncoder.Encode(entry.Packet, ipv4Packet, sourceMac))
            .ToArray();

        foreach ((TcpPacket packet, TcpState state) in outbound)
        {
            Console.WriteLine(
                $"TCP OUT {ipv4Packet.Destination.Value}:{packet.Port} " +
                $"-> {ipv4Packet.Source.Value}:{packet.DestinationPort} " +
                $"flags=[{(TcpFlag)packet.Flags}] " +
                $"seq={packet.SequenceNumber} " +
                $"ack={packet.AcknowledgmentNumber} " +
                $"payload={packet.Payload.Length}B " +
                $"state={state}");
        }

        return frames;
    }

    public TcpConnection GetOrCreateTcpConnection(
        (Ipv4Address SourceIp, Ipv4Address DestinationIp, ushort SourcePort, ushort DestinationPort) key)
    {
        if (_tcpConnections.TryGetValue(key, out TcpConnection? connection))
        {
            if (!connection.IsClosed && !connection.TryExpireTimeWait())
                return connection;

            _tcpConnections.Remove(key);
        }

        connection = new TcpConnection(
            key.DestinationPort,
            key.SourcePort,
            getCurrentTime: _getCurrentTime,
            timeWaitDuration: _timeWaitDuration);
        _tcpConnections[key] = connection;
        return connection;
    }
}
