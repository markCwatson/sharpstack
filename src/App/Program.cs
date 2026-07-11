using App.Network;
using App.Device;

namespace App;

class Program
{
    static async Task Main(string[] _)
    {
        var stack = new Stack();
        var device = new Tap();

        // done like this so it can be unit tested
        while (true)
            await StackRunner.ProcessOneFrame(stack, device);
    }
}
