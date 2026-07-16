using System.Text;
using App.Network.Tcp;

namespace App.Application;

public sealed class HttpApplication : IApplication
{
    public Task<ApplicationResult> HandleRequestAsync(TcpConnection connection)
    {
        byte[] bytes = connection.GetReceivedData();
        string request = Encoding.ASCII.GetString(bytes);
        int headerEnd = request.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (headerEnd < 0)
            return Task.FromResult(ApplicationResult.Empty);

        connection.ConsumeData(headerEnd + 4);

        int requestLineEnd = request.IndexOf("\r\n", StringComparison.Ordinal);
        if (requestLineEnd < 0)
            return CompletedResponse("400 Bad Request", "Bad Request\n");

        string requestLine = request[..requestLineEnd];
        string[] requestParts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestParts.Length < 2)
            return CompletedResponse("400 Bad Request", "Bad Request\n");

        if (requestParts[0] != "GET")
            return CompletedResponse("405 Method Not Allowed", "Method Not Allowed\n");

        if (requestParts[1] != "/")
            return CompletedResponse("404 Not Found", "Not Found\n");

        return CompletedResponse("200 OK", "Hello from sharpstack!\n");
    }

    private static Task<ApplicationResult> CompletedResponse(string status, string body) =>
        Task.FromResult(new ApplicationResult(CreateResponse(status, body), CloseConnection: true));

    private static byte[] CreateResponse(string status, string body)
    {
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        string headers = $"HTTP/1.1 {status}\r\nContent-Length: {bodyBytes.Length}\r\nContent-Type: text/plain; charset=utf-8\r\nConnection: close\r\n\r\n";
        return Encoding.ASCII.GetBytes(headers).Concat(bodyBytes).ToArray();
    }
}
