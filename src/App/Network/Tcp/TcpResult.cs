namespace App.Network.Tcp;

public sealed record TcpResult(
    IReadOnlyList<TcpPacket> Outbound,
    bool DataAvailable = false,
    bool PeerClosed = false)
{
    public static TcpResult Empty => new TcpResult(
            Outbound: Array.Empty<TcpPacket>(),
            DataAvailable: false,
            PeerClosed: false
        );
}
