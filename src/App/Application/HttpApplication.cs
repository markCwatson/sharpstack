using System.Text;
using App.Network.Tcp;

namespace App.Application;

public sealed class HttpApplication : IApplication
{
    // will need a wey to read files from disk

    public Task<byte[]> HandleRequestAsync(TcpConnection conn)
    {
        byte[] bytes = conn.GetReceivedData();
        Console.WriteLine($"HttpApplication.HandleRequestAsync entered with {bytes.Length} bytes");
        Console.WriteLine($"HTTP request bytes: {Encoding.ASCII.GetString(bytes)}");

        string request = Encoding.ASCII.GetString(bytes);
        int headerEnd = request.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (headerEnd < 0)
            return Task.FromResult(Array.Empty<byte>());

        int requestLineEnd = request.IndexOf("\r\n", StringComparison.Ordinal);
        string requestLine = request[..requestLineEnd];
        string[] requestParts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestParts.Length < 2)
            return Task.FromResult(CreateResponse("400 Bad Request", "Bad Request\n"));

        if (requestParts[0] != "GET")
            return Task.FromResult(CreateResponse("405 Method Not Allowed", "Method Not Allowed\n"));

        if (requestParts[1] != "/")
            return Task.FromResult(CreateResponse("404 Not Found", "Not Found\n"));

        conn.ConsumeData(headerEnd + 4); // +4 for the \r\n\r\n
        return Task.FromResult(CreateResponse("200 OK", "Hello from sharpstack!\n"));
    }

    private static byte[] CreateResponse(string status, string body)
    {
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        string headers = $"HTTP/1.1 {status}\r\nContent-Length: {bodyBytes.Length}\r\nContent-Type: text/plain; charset=utf-8\r\nConnection: close\r\n\r\n";
        return Encoding.ASCII.GetBytes(headers).Concat(bodyBytes).ToArray();
    }
}
