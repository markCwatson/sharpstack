using ScratchHttpServer.Network;
using ScratchHttpServer.Device;

namespace ScratchHttpServer;

class Program
{
    static async Task Main(string[] _)
    {
        var stack = new Stack();
        var devie = new Tap();

        while (true)
        {
            // todo: read a frame from the device
            // var bytes = await device.ReadFrameAsync();

            // todo: pass the frame to the stack
            // var incoming = EthernetFrame.Parse(bytes);
            // await stack.HandleFrameAsync(incoming, out var outgoing);

            // todo: write any outgoing frames back to the device
            // await device.WriteFrameAsync(outgoing.ToBytes());
        }
    }
}
