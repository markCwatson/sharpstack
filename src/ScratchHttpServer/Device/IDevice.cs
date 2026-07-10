using ScratchHttpServer.Network.Ethernet;

namespace ScratchHttpServer.Device;

public interface IDevice
{
    public abstract Task<EthernetFrame> ReadFrameAsync();
    public abstract Task WriteFrameAsync(EthernetFrame frame);
}
