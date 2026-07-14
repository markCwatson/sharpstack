using App.Network.Ethernet;

namespace App.Device;

public interface IDevice
{
    public abstract Task<byte[]> ReadEthernetFrameAsync();
    public abstract Task WriteEthernetFrameAsync(EthernetFrame frame);
}
