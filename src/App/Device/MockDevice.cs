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

    public Task<byte[]> ReadFrameAsync()
    {
        return Task.FromResult(_data);
    }

    public Task WriteFrameAsync(EthernetFrame frame)
    {
        WrittenFrame = frame;
        return Task.CompletedTask;
    }
}
