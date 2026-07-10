using App.Network.Ethernet;

namespace App.Device;

public sealed class Tap() : IDevice
{
    public Task<EthernetFrame> ReadFrameAsync()
    {
        throw new NotImplementedException();
    }

    public Task WriteFrameAsync(EthernetFrame frame)
    {
        throw new NotImplementedException();
    }
}
