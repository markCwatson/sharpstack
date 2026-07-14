using App.Network.Tcp;

namespace App.Application;

// an application will take a tcp connection and a TcpPacket and do something with it
// returning a response byte stream to send back to the client
public interface IApplication
{
    abstract Task<byte[]> HandleRequestAsync(TcpConnection conn, TcpPacket packet);
}
