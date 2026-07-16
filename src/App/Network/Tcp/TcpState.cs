namespace App.Network.Tcp;

public enum TcpState
{
    Closed,
    Listen,
    SynReceived,
    Established,
    FinWait1,
    FinWait2,
    CloseWait,
    Closing,
    LastAck,
    TimeWait
}
