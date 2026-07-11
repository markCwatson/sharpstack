using App.Device;
using App.Network;
using App.Network.Ethernet;

namespace App;

public sealed class StackRunner
{
    // this method is used in the unit tests to process one frame at a time
    public static async Task ProcessOneFrame(Stack stack, IDevice device)
    {
        byte[] bytes = await device.ReadFrameAsync();
        EthernetFrame? outgoing = await stack.HandleFrameAsync(bytes);
        if (outgoing is not null)
            await device.WriteFrameAsync(outgoing.Value);
    }
}
