using App.Network.Ethernet;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace App.Device;

public sealed class Tap : IDevice
{
    private readonly string _name = "rawstack0"; // <-- this assumes you ran the tap-setup.sh script to create the TAP named "rawstack0"
    private readonly FileStream _stream;
    private const int OReadWrite = 2;
    private const short IffTap = 0x0002;
    private const short IffNoPi = 0x1000;
    private const ulong TunSetIff = 0x400454ca;

    // note: this constructor was created by AI
    // it opens the TAP device and configures it with the specified name and flags
    // then creates a FileStream to read/write to the TAP device
    public Tap()
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("This example runs against Linux /dev/net/tun. Run it on Linux with a TAP device.");

        // Linux creates the TAP here "/dev/net/tun" ... and everything is a file in Linux, so we can use FileStream to read/write to it.
        var fileDescriptor = open("/dev/net/tun", OReadWrite);
        if (fileDescriptor < 0)
            throw new IOException("Could not open /dev/net/tun. Try running with sudo or check that tun/tap support is installed.");

        var request = new byte[40];
        var nameBytes = System.Text.Encoding.ASCII.GetBytes(_name);
        nameBytes.AsSpan(0, Math.Min(nameBytes.Length, 15)).CopyTo(request.AsSpan(0, 16));
        BitConverter.GetBytes((short)(IffTap | IffNoPi)).CopyTo(request.AsSpan(16, 2));

        // The ioctl call configures the TAP device with the specified name and flags.
        if (ioctl(fileDescriptor, TunSetIff, request) < 0)
        {
            var error = close(fileDescriptor);
            throw new IOException($"Could not configure TAP device {_name}. Error code: {Marshal.GetLastWin32Error()}, close returned: {error}");
        }

        var handle = new SafeFileHandle(fileDescriptor, ownsHandle: true);
        _stream = new FileStream(handle, FileAccess.ReadWrite, bufferSize: 2048, isAsync: false);
    }

    public async Task<byte[]> ReadFrameAsync()
    {
        byte[] buffer = new byte[2048];
        var numBytes = await _stream.ReadAsync(buffer);
        return buffer[..numBytes];
    }

    public async Task WriteFrameAsync(EthernetFrame frame) => await _stream.WriteAsync(frame.ToBytes());

    // note : AI generated these DLL imports to be used in the ctor too
    [DllImport("libc", SetLastError = true)]
    private static extern int open(string pathname, int flags);

    [DllImport("libc", SetLastError = true)]
    private static extern int ioctl(int fileDescriptor, ulong request, byte[] argp);

    [DllImport("libc", SetLastError = true)]
    private static extern int close(int fileDescriptor);
}
