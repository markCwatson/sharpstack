namespace App.Network.Tcp;

// TCP state, sequence numbers, and accepted receive bytes.
// Note: gpt-5.6 helped me massively with this the tcp protocol.... it was a good learning exercise.
public sealed class TcpConnection
{
    public static TimeSpan DefaultTimeWaitDuration { get; } = TimeSpan.FromMinutes(1);

    private readonly ushort _localPort;
    private readonly ushort _remotePort;
    private readonly List<byte> _receivedData = [];
    private readonly Func<DateTimeOffset> _getCurrentTime;
    private readonly TimeSpan _timeWaitDuration;
    private uint _sendUnacknowledged;
    private uint _sendNext; // this is the next sequence number we will send, and is used to determine if we can send more data
    private uint _receiveNext; // this is what we expect the next sequence number to be from the peer, and is used to determine if we can accept the data
    private DateTimeOffset? _timeWaitExpiresAt;
    private TcpState _state = TcpState.Listen;

    public TcpConnection(
        ushort localPort = 0,
        ushort remotePort = 0,
        uint initialSequenceNumber = 10_000,
        Func<DateTimeOffset>? getCurrentTime = null,
        TimeSpan? timeWaitDuration = null)
    {
        _localPort = localPort;
        _remotePort = remotePort;
        _sendUnacknowledged = initialSequenceNumber;
        _sendNext = initialSequenceNumber;
        _getCurrentTime = getCurrentTime ?? (() => DateTimeOffset.UtcNow);
        _timeWaitDuration = timeWaitDuration ?? DefaultTimeWaitDuration;
    }

    public TcpState State => _state;
    public bool IsEstablished => _state == TcpState.Established;
    public bool IsClosed => _state == TcpState.Closed;

    public byte[] GetReceivedData() => _receivedData.ToArray();

    public void ConsumeData(int count)
    {
        if (count < 0 || count > _receivedData.Count)
            throw new ArgumentOutOfRangeException(nameof(count), "Cannot consume more data than received.");

        _receivedData.RemoveRange(0, count);
    }

    public TcpResult Receive(TcpPacket packet)
    {
        return _state switch
        {
            TcpState.Listen => ReceiveInListenState(packet),
            TcpState.SynReceived => ReceiveInSynReceivedState(packet),
            TcpState.Established => ReceiveInEstablishedState(packet),
            TcpState.FinWait1 => ReceiveInFinWait1State(packet),
            TcpState.FinWait2 => ReceiveInFinWait2State(packet),
            TcpState.CloseWait => ReceiveInCloseWaitState(packet),
            TcpState.Closing => ReceiveInClosingState(packet),
            TcpState.LastAck => ReceiveInLastAckState(packet),
            TcpState.TimeWait => ReceiveInTimeWaitState(packet),
            TcpState.Closed => TcpResult.Empty,
            _ => throw new InvalidOperationException($"Invalid TCP state: {_state}")
        };
    }

    public TcpResult Send(byte[] payload)
    {
        if (_state is not (TcpState.Established or TcpState.CloseWait))
            throw new InvalidOperationException($"Cannot send data while in {_state}.");

        if (payload.Length == 0)
            return TcpResult.Empty;

        return new TcpResult([CreateOutboundSegmentAndAdvanceSequence(TcpFlag.PSH | TcpFlag.ACK, payload)]);
    }

    public TcpResult Close()
    {
        switch (_state)
        {
            case TcpState.Established:
                _state = TcpState.FinWait1;
                break;
            case TcpState.CloseWait:
                _state = TcpState.LastAck;
                break;
            default:
                return TcpResult.Empty;
        }

        return new TcpResult([CreateOutboundSegmentAndAdvanceSequence(TcpFlag.FIN | TcpFlag.ACK, [])]);
    }

    public bool TryExpireTimeWait()
    {
        if (_state != TcpState.TimeWait || _timeWaitExpiresAt is null || _getCurrentTime() < _timeWaitExpiresAt)
            return false;

        _state = TcpState.Closed;
        _timeWaitExpiresAt = null;
        return true;
    }

    private TcpResult ReceiveInListenState(TcpPacket packet)
    {
        if ((packet.Flags & (byte)TcpFlag.SYN) == 0)
            return TcpResult.Empty;

        _receiveNext = packet.SequenceNumber + 1;
        _state = TcpState.SynReceived;
        return new TcpResult([CreateOutboundSegmentAndAdvanceSequence(TcpFlag.SYN | TcpFlag.ACK, [])]);
    }

    private TcpResult ReceiveInSynReceivedState(TcpPacket packet)
    {
        if ((packet.Flags & (byte)TcpFlag.ACK) == 0 || packet.AcknowledgmentNumber != _sendNext)
            return TcpResult.Empty;

        _sendUnacknowledged = packet.AcknowledgmentNumber;
        _state = TcpState.Established;
        return TcpResult.Empty; // we expect two INs in a row here
    }

    private TcpResult ReceiveInEstablishedState(TcpPacket packet)
    {
        AcceptAcknowledgment(packet);

        if (packet.Payload.Length == 0 && (packet.Flags & (byte)TcpFlag.SYN) == 0 && (packet.Flags & (byte)TcpFlag.FIN) == 0)
            return TcpResult.Empty;

        if (!TryToReadPayload(packet, out byte[] payloadData))
            return new TcpResult([CreateOutboundSegmentAndAdvanceSequence(TcpFlag.ACK, [])]);

        if (payloadData.Length > 0)
            _receivedData.AddRange(payloadData);

        bool finReceived = (packet.Flags & (byte)TcpFlag.FIN) != 0;
        if (finReceived)
            _state = TcpState.CloseWait;

        return new TcpResult(
            Outbound: [CreateOutboundSegmentAndAdvanceSequence(TcpFlag.ACK, [])],
            DataAvailable: payloadData.Length > 0,
            PeerClosed: finReceived);
    }

    private TcpResult ReceiveInFinWait1State(TcpPacket packet)
    {
        bool finAcknowledged = AcceptAcknowledgment(packet) && _sendUnacknowledged == _sendNext;

        if ((packet.Flags & (byte)TcpFlag.FIN) == 0)
        {
            if (finAcknowledged)
                _state = TcpState.FinWait2;

            return TcpResult.Empty;
        }

        if (!TryToReadPayload(packet, out _))
            return new TcpResult([CreateOutboundSegmentAndAdvanceSequence(TcpFlag.ACK, [])]);

        if (finAcknowledged)
            EnterTimeWait();
        else
            _state = TcpState.Closing;

        return new TcpResult([CreateOutboundSegmentAndAdvanceSequence(TcpFlag.ACK, [])], PeerClosed: true);
    }

    private TcpResult ReceiveInFinWait2State(TcpPacket packet)
    {
        AcceptAcknowledgment(packet);

        if ((packet.Flags & (byte)TcpFlag.FIN) == 0)
            return TcpResult.Empty;

        if (!TryToReadPayload(packet, out _))
            return new TcpResult([CreateOutboundSegmentAndAdvanceSequence(TcpFlag.ACK, [])]);

        EnterTimeWait();
        return new TcpResult([CreateOutboundSegmentAndAdvanceSequence(TcpFlag.ACK, [])], PeerClosed: true);
    }

    private TcpResult ReceiveInCloseWaitState(TcpPacket packet)
    {
        AcceptAcknowledgment(packet);

        if ((packet.Flags & (byte)TcpFlag.FIN) != 0)
            return new TcpResult([CreateOutboundSegmentAndAdvanceSequence(TcpFlag.ACK, [])]);

        return TcpResult.Empty;
    }

    private TcpResult ReceiveInClosingState(TcpPacket packet)
    {
        if (AcceptAcknowledgment(packet) && _sendUnacknowledged == _sendNext)
            EnterTimeWait();

        return TcpResult.Empty;
    }

    private TcpResult ReceiveInLastAckState(TcpPacket packet)
    {
        if (AcceptAcknowledgment(packet) && _sendUnacknowledged == _sendNext)
        {
            _state = TcpState.Closed;
            return TcpResult.Empty;
        }

        if ((packet.Flags & (byte)TcpFlag.FIN) != 0)
            return new TcpResult([CreateOutboundSegmentAndAdvanceSequence(TcpFlag.ACK, [])]);

        return TcpResult.Empty;
    }

    private TcpResult ReceiveInTimeWaitState(TcpPacket packet)
    {
        if ((packet.Flags & (byte)TcpFlag.FIN) == 0)
            return TcpResult.Empty;

        EnterTimeWait();
        return new TcpResult([CreateOutboundSegmentAndAdvanceSequence(TcpFlag.ACK, [])]);
    }

    private void EnterTimeWait()
    {
        _state = TcpState.TimeWait;
        _timeWaitExpiresAt = _getCurrentTime() + _timeWaitDuration;
    }

    private bool TryToReadPayload(TcpPacket packet, out byte[] acceptedData)
    {
        acceptedData = Array.Empty<byte>();

        if (packet.SequenceNumber != _receiveNext)
            return false;

        acceptedData = packet.Payload;
        _receiveNext += (uint)packet.Payload.Length;

        if ((packet.Flags & (byte)TcpFlag.SYN) != 0)
            _receiveNext++;

        if ((packet.Flags & (byte)TcpFlag.FIN) != 0)
            _receiveNext++;

        return true;
    }

    private bool AcceptAcknowledgment(TcpPacket packet)
    {
        if ((packet.Flags & (byte)TcpFlag.ACK) == 0)
            return false;

        if (packet.AcknowledgmentNumber < _sendUnacknowledged || packet.AcknowledgmentNumber > _sendNext)
            return false;

        _sendUnacknowledged = packet.AcknowledgmentNumber;
        return true;
    }

    private TcpPacket CreateOutboundSegmentAndAdvanceSequence(TcpFlag flags, byte[] payload)
    {
        var packet = new TcpPacket(
            _localPort,
            _remotePort,
            _sendNext,
            _receiveNext,
            5,
            (byte)flags,
            ushort.MaxValue,
            0,
            0,
            payload);

        _sendNext += (uint)payload.Length;

        if ((flags & TcpFlag.SYN) != 0)
            _sendNext++;

        if ((flags & TcpFlag.FIN) != 0)
            _sendNext++;

        return packet;
    }
}
