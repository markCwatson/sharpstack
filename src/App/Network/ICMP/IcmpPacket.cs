using App.Network.Ethernet;
using App.Network.IPv4;
using System.Buffers.Binary;

namespace App.Network.ICMP;

// an IcmpPacket has the foloowing fields (8 bytes)
// Type: 1 byte
// Code: 1 byte
// Checksum: 2 bytes
// Identifier: 2 bytes
// Sequence number: 2 bytes
// Payload: variable length
public sealed record IcmpPacket(byte Type, byte Code, ushort Checksum, ushort Identifier, ushort SequenceNumber, byte[] Payload)
{
    public IcmpCode CodeEnum => (IcmpCode)Code;

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

    public static async Task<EthernetFrame?> HandlePacket(IPv4Packet packet)
    {
        // parse

        // check for echo request

        // create an echo reply icmp packet

        // return an EthernetFrame
        return null;
    }
}
