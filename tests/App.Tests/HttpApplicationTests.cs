using System.Text;
using App.Application;
using App.Network.Tcp;

namespace App.Tests;

public class HttpApplicationTests
{
    [Fact]
    public async Task HandleRequestAsync_CompleteRootRequest_ReturnsOkResponse()
    {
        var connection = new TcpConnection();
        connection.ReceiveData(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: 10.0.0.2\r\n\r\n"));

        byte[] response = await new HttpApplication().HandleRequestAsync(connection);
        string responseText = Encoding.UTF8.GetString(response);

        Assert.StartsWith("HTTP/1.1 200 OK\r\n", responseText);
        Assert.Contains("Content-Length: 23\r\n", responseText);
        Assert.EndsWith("\r\n\r\nHello from sharpstack!\n", responseText);
    }

    [Fact]
    public async Task HandleRequestAsync_IncompleteHeaders_ReturnsNoResponse()
    {
        var connection = new TcpConnection();
        connection.ReceiveData(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: 10.0.0.2\r\n"));

        byte[] response = await new HttpApplication().HandleRequestAsync(connection);

        Assert.Empty(response);
    }
}