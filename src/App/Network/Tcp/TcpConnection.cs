using System.Buffers.Binary;
using App.Network.Ethernet;
using App.Network.IPv4;

namespace App.Network.Tcp;

public sealed class TcpConnection
{
    private TcpState _state = TcpState.Listen;
    public bool IsEstablished => _state == TcpState.Established;
    private uint _sequenceNumber = 10_000;
    private uint _acknowledgmentNumber;
    private readonly List<byte> _receivedData = new();

    // steam-like interface for tcp server to read data and the application to consume it
    public byte[] GetReceivedData() => _receivedData.ToArray();
    public void ReceiveData(byte[] data) => _receivedData.AddRange(data);
    public void ConsumeData(int count)
    {
        if (count > _receivedData.Count)
            throw new ArgumentException("Cannot consume more data than received.");
        _receivedData.RemoveRange(0, count);
    }

    public EthernetFrame? UpdateState(IPv4Packet ipv4Packet, TcpPacket tcpPacket, MacAddress remoteMac)
    {
        if (_state == TcpState.Listen && tcpPacket.Flags == (byte)TcpFlag.SYN)
        {
            _state = TcpState.SynReceived;
            _acknowledgmentNumber = tcpPacket.SequenceNumber + 1;
            return CreateEthernetFrame(ipv4Packet, tcpPacket, Stack.MacAddress, remoteMac);
        }
        else if (_state == TcpState.SynReceived && (tcpPacket.Flags & (byte)TcpFlag.ACK) != 0 && tcpPacket.AcknowledgmentNumber == _sequenceNumber + 1)
        {
            _sequenceNumber++;
            _state = TcpState.Established;
        }
        else if (_state == TcpState.Established && (tcpPacket.Flags & (byte)TcpFlag.FIN) != 0)
        {
            _state = TcpState.CloseWait;
        }

        if (tcpPacket.Payload.Length > 0 || (tcpPacket.Flags & (byte)TcpFlag.FIN) != 0)
        {
            _acknowledgmentNumber = tcpPacket.SequenceNumber + (uint)tcpPacket.Payload.Length;

            if ((tcpPacket.Flags & (byte)TcpFlag.FIN) != 0)
                _acknowledgmentNumber++;
        }

        return null;
    }

    private EthernetFrame CreateEthernetFrame(IPv4Packet ipv4Packet, TcpPacket packet, MacAddress sourceMac, MacAddress destinationMac)
    {
        var res = new TcpPacket(
                packet.DestinationPort,
                packet.Port,
                _sequenceNumber,
                _acknowledgmentNumber,
                packet.DataOffset,
                (byte)(TcpFlag.SYN | TcpFlag.ACK),
                0,
                0, // checksum
                0,
                Array.Empty<byte>()
        );

        byte[] bytes = res.ToBytes();
        // apparentluy tcp/ipv4 checksums require this
        PseudoHeader pseudoHeader = new PseudoHeader(ipv4Packet.Destination, ipv4Packet.Source, 0, (byte)Ipv4Protocol.TCP, (ushort)bytes.Length);
        ushort tcpChecksum = Utils.Checksum.Calculate(pseudoHeader.ToBytes().Concat(res.ToBytes()).ToArray());
        res = res with { Checksum = tcpChecksum };

        IPv4Packet ipv4Wrap = new IPv4Packet(
            ipv4Packet.Version,
            ipv4Packet.HeaderLength,
            ipv4Packet.TypeOfService,
            (ushort)(ipv4Packet.HeaderLength * 4 + res.ToBytes().Length),
            ipv4Packet.Identification,
            ipv4Packet.Flags,
            ipv4Packet.FragmentOffset,
            ipv4Packet.TimeToLive,
            ipv4Packet.Protocol,
            0, // checksum
            ipv4Packet.Destination,
            ipv4Packet.Source,
            res.ToBytes()
        );

        byte[] inBytes = ipv4Wrap.ToBytes();
        int headerLength = ipv4Wrap.HeaderLength * 4;
        ushort ipv4WrapChecksum = Utils.Checksum.Calculate(inBytes[..headerLength]);
        ipv4Wrap = ipv4Wrap with { HeaderChecksum = ipv4WrapChecksum };

        return new EthernetFrame(
            destinationMac,
            sourceMac,
            (ushort)EtherType.IPv4,
            ipv4Wrap.ToBytes()
        );
    }

    private enum TcpState
    {
        Closed,
        Listen,
        SynSent,
        SynReceived,
        Established,
        FinWait1,
        FinWait2,
        CloseWait,
        Closing,
        LastAck,
        TimeWait
    }

    private enum TcpFlag : byte
    {
        FIN = 0x01,
        SYN = 0x02,
        RST = 0x04,
        PSH = 0x08,
        ACK = 0x10,
        URG = 0x20,
        ECE = 0x40,
        CWR = 0x80
    }

    private record PseudoHeader(Ipv4Address Source, Ipv4Address Destination, byte Zero, byte Protocol, ushort TcpLength)
    {
        public byte[] ToBytes()
        {
            byte[] bytes = new byte[12];
            Array.Copy(Source.ToArray(), 0, bytes, 0, 4);
            Array.Copy(Destination.ToArray(), 0, bytes, 4, 4);
            bytes[8] = Zero;
            bytes[9] = Protocol;
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(10, 2), TcpLength);
            return bytes;
        }
    }
}
