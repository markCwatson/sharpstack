using App.Network.Ethernet;

namespace App.Device;

public interface IDevice
{
    public abstract Task<EthernetFrame> ReadFrameAsync();
    public abstract Task WriteFrameAsync(EthernetFrame frame);
}
