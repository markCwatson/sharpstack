using ScratchHttpServer.Network;
using ScratchHttpServer.Device;

namespace ScratchHttpServer;

class Program
{
    static async Task Main(string[] _)
    {
        var stack = new Stack();
        var device = new Tap();

        while (true)
        {
            var bytes = await device.ReadFrameAsync();
            var outgoing = await stack.HandleFrameAsync(bytes);
            await device.WriteFrameAsync(outgoing);
        }
    }
}
