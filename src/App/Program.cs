using App.Network;
using App.Device;
using App.Network.Ethernet;

namespace App;

class Program
{
    static async Task Main(string[] _)
    {
        var stack = new Stack();
        var device = new Tap();

        while (true)
        {
            byte[] bytes = await device.ReadFrameAsync();
            EthernetFrame outgoing = await stack.HandleFrameAsync(bytes);
            await device.WriteFrameAsync(outgoing);
        }
    }
}
