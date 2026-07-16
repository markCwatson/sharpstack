using App.Network.Tcp;

namespace App.Application;

// An application reads the connection byte stream and decides whether a response closes it.
public interface IApplication
{
    Task<ApplicationResult> HandleRequestAsync(TcpConnection connection);
}
