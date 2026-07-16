using System.Text;
using App.Application;
using App.Network.Tcp;

namespace App.Tests;

public class HttpApplicationTests
{
    [Fact]
    public async Task HandleRequestAsync_CompleteRootRequest_ReturnsOkResponse()
    {
        TcpConnection connection = ConnectionWithData("GET / HTTP/1.1\r\nHost: 10.0.0.2\r\n\r\n");

        ApplicationResult result = await new HttpApplication().HandleRequestAsync(connection);
        string responseText = Encoding.UTF8.GetString(result.Response);

        Assert.StartsWith("HTTP/1.1 200 OK\r\n", responseText);
        Assert.Contains("Content-Length: 23\r\n", responseText);
        Assert.EndsWith("\r\n\r\nHello from sharpstack!\n", responseText);
        Assert.True(result.CloseConnection);
        Assert.Empty(connection.GetReceivedData());
    }

    [Fact]
    public async Task HandleRequestAsync_IncompleteHeaders_ReturnsNoResponse()
    {
        TcpConnection connection = ConnectionWithData("GET / HTTP/1.1\r\nHost: 10.0.0.2\r\n");

        ApplicationResult result = await new HttpApplication().HandleRequestAsync(connection);

        Assert.Empty(result.Response);
        Assert.False(result.CloseConnection);
        Assert.NotEmpty(connection.GetReceivedData());
    }

    private static TcpConnection ConnectionWithData(string data)
    {
        var connection = new TcpConnection(80, 49152);
        connection.Receive(Packet(123, 0, TcpFlag.SYN));
        connection.Receive(Packet(124, 10_001, TcpFlag.ACK));
        connection.Receive(Packet(124, 10_001, TcpFlag.PSH | TcpFlag.ACK, Encoding.ASCII.GetBytes(data)));
        return connection;
    }

    private static TcpPacket Packet(uint sequence, uint acknowledgment, TcpFlag flags, byte[]? payload = null) =>
        new(49152, 80, sequence, acknowledgment, 5, (byte)flags, ushort.MaxValue, 0, 0, payload ?? []);
}