using App.Device;
using App.Network;
using App.Network.Ethernet;

namespace App;

public sealed class StackRunner
{
    // this method is used in the unit tests to process one frame at a time
    public static async Task ProcessOneEthernetFrame(Stack stack, IDevice device)
    {
        byte[] bytes = await device.ReadEthernetFrameAsync();

        IReadOnlyList<EthernetFrame> outgoing = await stack.HandleEthernetFrameAsync(bytes);
        foreach (var frame in outgoing)
            await device.WriteEthernetFrameAsync(frame);
    }
}
