using App.Network.Tcp;

namespace App.Application;

public sealed class HttpApplication : IApplication
{
    // will need a wey to read files from disk

    public Task<byte[]> HandleRequestAsync(TcpConnection conn)
    {
        // read data from the connection

        byte[] bytes = conn.GetReceivedData();
        Console.WriteLine($"HttpApplication.HandleRequestAsync entered with {bytes.Length} bytes");
        Console.WriteLine($"HTTP request bytes: {System.Text.Encoding.ASCII.GetString(bytes)}");

        // wait until we have the complte header which ends in \r\n\r\n

        // convert bytes to text 

        // split by \r\n to get the first line, then split by space to get the method and path

        // should we handle more than the GET method?

        return Task.FromResult(Array.Empty<byte>());
    }
}
