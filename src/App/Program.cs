using App.Network;
using App.Device;
using App.Application;

namespace App;

class Program
{
    static async Task Main(string[] _)
    {
        var stack = new Stack();
        var device = new Tap();

        // create and register applications
        IApplication http = new HttpApplication();
        stack.RegisterTcpListener(80, http);

        // done like this so it can be unit tested
        while (true)
            await StackRunner.ProcessOneEthernetFrame(stack, device);
    }
}
