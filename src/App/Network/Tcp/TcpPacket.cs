using App.Network.Ethernet;
using App.Network.IPv4;
using App.Application;
using System.Buffers.Binary;

namespace App.Network.Tcp;

// a tcp packet contains: (20 bytes header + payload)
// Source port - 2 bytes
// Destination port - 2 bytes
// Sequence number - 4 bytes
// Acknowledgment number - 4 bytes
// Data offset and flags - 2 bytes (URG, ACK, PSH, RST, SYN, FIN)
// Window size - 2 bytes
// Checksum - 2 bytes
// Urgent pointer - 2 bytes
// TCP payload
public sealed record TcpPacket(ushort Port,
                               ushort DestinationPort,
                               uint SequenceNumber,
                               uint AcknowledgmentNumber,
                               ushort DataOffset,
                               byte Flags,
                               ushort WindowSize,
                               ushort Checksum,
                               ushort UrgentPointer,
                               byte[] Payload) : IPacket<TcpPacket>
{
    public static TcpPacket Parse(byte[] bytes)
    {
        if (bytes.Length < 20)
            throw new ArgumentException("TCP packet must be at least 20 bytes long.");

        ushort dataOffsetAndFlags = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(12, 2));
        ushort dataOffset = (ushort)(dataOffsetAndFlags >> 12);
        byte flags = (byte)(dataOffsetAndFlags & 0x0FFF);

        if (dataOffset < 5)
            throw new ArgumentException("TCP data offset must be at least 5.");

        int headerLength = dataOffset * 4;

        if (headerLength > bytes.Length)
            throw new ArgumentException("TCP header extends beyond packet.");

        return new TcpPacket(
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(0, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(2, 2)),
            BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(4, 4)),
            BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(8, 4)),
            dataOffset,
            flags,
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(14, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(16, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(18, 2)),
            bytes[headerLength..]
        );
    }

    public byte[] ToBytes()
    {
        var bytes = new byte[DataOffset * 4 + Payload.Length];

        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(0, 2), Port);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2, 2), DestinationPort);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4, 4), SequenceNumber);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), AcknowledgmentNumber);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(12, 2), (ushort)((DataOffset << 12) | (Flags & 0x0FFF)));
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(14, 2), WindowSize);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(16, 2), Checksum);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(18, 2), UrgentPointer);

        Array.Copy(Payload, 0, bytes, DataOffset * 4, Payload.Length);

        return bytes;
    }
}
