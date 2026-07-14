using System.Buffers.Binary;

namespace App.Network.ICMP;

// an IcmpPacket has the foloowing fields (8 bytes)
// Type: 1 byte
// Code: 1 byte
// Checksum: 2 bytes
// Identifier: 2 bytes
// Sequence number: 2 bytes
// Payload: variable length
public sealed record IcmpPacket(byte Type,
                                byte Code,
                                ushort Checksum,
                                ushort Identifier,
                                ushort SequenceNumber,
                                byte[] Payload) : IPacket<IcmpPacket>
{
    public IcmpCode CodeEnum => (IcmpCode)Code;
    public IcmpType TypeEnum => (IcmpType)Type;

    public static IcmpPacket Parse(byte[] bytes)
    {
        if (bytes.Length < 8)
            throw new ArgumentException("ICMP packet must be at least 8 bytes long");

        return new IcmpPacket(
            bytes[0],
            bytes[1],
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(2, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(4, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(6, 2)),
            bytes[8..]
        );
    }

    public byte[] ToBytes()
    {
        var bytes = new byte[8 + Payload.Length];
        bytes[0] = Type;
        bytes[1] = Code;
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2, 2), Checksum);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(4, 2), Identifier);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(6, 2), SequenceNumber);
        Payload.CopyTo(bytes.AsSpan(8));
        return bytes;
    }
}
