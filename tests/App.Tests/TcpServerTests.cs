using App.Application;
using App.Network;
using App.Network.Ethernet;
using App.Network.IPv4;
using App.Network.Tcp;

namespace App.Tests;

public class TcpServerTests
{
    [Fact]
    public void GetOrCreateTcpConnection_ReusesConnectionForSameFourTuple()
    {
        var server = new TcpServer();
        var remoteIp = new Ipv4Address("10.0.0.1");

        TcpConnection first = server.GetOrCreateTcpConnection(remoteIp, Stack.Ipv4Address, 49152, 80);
        TcpConnection second = server.GetOrCreateTcpConnection(remoteIp, Stack.Ipv4Address, 49152, 80);
        TcpConnection otherPort = server.GetOrCreateTcpConnection(remoteIp, Stack.Ipv4Address, 49153, 80);

        Assert.Same(first, second);
        Assert.NotSame(first, otherPort);
    }

    [Fact]
    public async Task HandlePacket_ForUnregisteredPort_ReturnsNull()
    {
        var server = new TcpServer();
        var syn = new TcpPacket(49152, 80, 123, 0, 5, 0x02, 0, 0, 0, Array.Empty<byte>());

        EthernetFrame? response = await server.HandlePacket(CreateIpv4Packet(syn), PeerMac, Stack.MacAddress);

        Assert.Null(response);
    }

    [Fact]
    public async Task HandlePacket_AccumulatesEstablishedPayloadBeforeDispatchingToListener()
    {
        var server = new TcpServer();
        var application = new RecordingApplication();
        server.RegisterTcpListener(80, application);

        var syn = new TcpPacket(49152, 80, 123, 0, 5, 0x02, 0, 0, 0, Array.Empty<byte>());
        EthernetFrame? synAck = await server.HandlePacket(CreateIpv4Packet(syn), PeerMac, Stack.MacAddress);

        var ack = new TcpPacket(49152, 80, 124, 10_001, 5, 0x10, 0, 0, 0, Array.Empty<byte>());
        await server.HandlePacket(CreateIpv4Packet(ack), PeerMac, Stack.MacAddress);

        var firstRequestSegment = new TcpPacket(49152, 80, 124, 10_001, 5, 0x18, 0, 0, 0, new byte[] { 0x47, 0x45 });
        await server.HandlePacket(CreateIpv4Packet(firstRequestSegment), PeerMac, Stack.MacAddress);

        var secondRequestSegment = new TcpPacket(49152, 80, 126, 10_001, 5, 0x18, 0, 0, 0, new byte[] { 0x54 });
        await server.HandlePacket(CreateIpv4Packet(secondRequestSegment), PeerMac, Stack.MacAddress);

        Assert.NotNull(synAck);
        Assert.Equal(2, application.Requests.Count);
        Assert.Equal(firstRequestSegment.Port, application.Requests[0].Packet.Port);
        Assert.Equal(firstRequestSegment.DestinationPort, application.Requests[0].Packet.DestinationPort);
        Assert.Equal(firstRequestSegment.Payload, application.Requests[0].Packet.Payload);
        Assert.Equal(new byte[] { 0x47, 0x45 }, application.Requests[0].BufferedData);
        Assert.Equal(secondRequestSegment.Port, application.Requests[1].Packet.Port);
        Assert.Equal(secondRequestSegment.DestinationPort, application.Requests[1].Packet.DestinationPort);
        Assert.Equal(secondRequestSegment.Payload, application.Requests[1].Packet.Payload);
        Assert.Equal(new byte[] { 0x47, 0x45, 0x54 }, application.Requests[1].BufferedData);
    }

    private static MacAddress PeerMac { get; } = new MacAddress(0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);

    private static IPv4Packet CreateIpv4Packet(TcpPacket tcpPacket)
    {
        byte[] payload = tcpPacket.ToBytes();
        return new IPv4Packet(
            4, 5, 0, (ushort)(20 + payload.Length), 0, 0, 0, 64, (byte)Ipv4Protocol.TCP, 0,
            new Ipv4Address("10.0.0.1"), Stack.Ipv4Address, payload);
    }

    private sealed class RecordingApplication : IApplication
    {
        public List<(TcpPacket Packet, byte[] BufferedData)> Requests { get; } = [];

        public Task<byte[]> HandleRequestAsync(TcpConnection connection, TcpPacket packet)
        {
            Requests.Add((packet, connection.GetReceivedData()));
            return Task.FromResult(Array.Empty<byte>());
        }
    }
}