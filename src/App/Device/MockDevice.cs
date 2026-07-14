using App.Network.Ethernet;

namespace App.Device;

public sealed class MockDevice : IDevice
{
    private readonly byte[] _data;
    public EthernetFrame? WrittenFrame { get; private set; }

    public MockDevice(byte[] data)
    {
        _data = data;
    }

    public Task<byte[]> ReadEthernetFrameAsync()
    {
        return Task.FromResult(_data);
    }

    public Task WriteEthernetFrameAsync(EthernetFrame frame)
    {
        WrittenFrame = frame;
        return Task.CompletedTask;
    }
}
