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
        EthernetFrame? outgoing = await stack.HandleEthernetFrameAsync(bytes);
        if (outgoing is not null)
        {
            Console.WriteLine($"Writing outgoing Ethernet frame: {outgoing.Value.Source} -> {outgoing.Value.Destination}, payload={outgoing.Value.Payload.Length} bytes");
            await device.WriteEthernetFrameAsync(outgoing.Value);
        }
        else
        {
            Console.WriteLine("No outgoing Ethernet frame");
        }
    }
}
