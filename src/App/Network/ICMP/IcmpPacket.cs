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

    public static async Task<EthernetFrame?> HandlePacket(IPv4Packet packet, MacAddress sourceMac, MacAddress destinationMac)
    {
        IcmpPacket incoming = Parse(packet.Payload);

        if (incoming.TypeEnum != IcmpType.EchoRequest)
            return null;

        IcmpPacket reply = new IcmpPacket(
            (byte)IcmpType.EchoReply,
            (byte)IcmpCode.EchoReply,
            0, // checksum
            incoming.Identifier,
            incoming.SequenceNumber,
            incoming.Payload
        );

        byte[] replyInBytes = reply.ToBytes();
        ushort replyChecksum = Utils.Checksum.Calculate(replyInBytes);
        reply = reply with { Checksum = replyChecksum };

        IPv4Packet ipv4Wrap = new IPv4Packet(
            packet.Version,
            packet.HeaderLength,
            packet.TypeOfService,
            (ushort)(packet.HeaderLength * 4 + reply.ToBytes().Length),
            packet.Identification,
            packet.Flags,
            packet.FragmentOffset,
            packet.TimeToLive,
            packet.Protocol,
            0, // checksum
            packet.Destination,
            packet.Source,
            reply.ToBytes() // wrapped in IPv4 packet
        );

        byte[] inBytes = ipv4Wrap.ToBytes();
        ushort ipv4WrapChecksum = Utils.Checksum.Calculate(inBytes);
        ipv4Wrap = ipv4Wrap with { HeaderChecksum = ipv4WrapChecksum };

        return new EthernetFrame(
            // destination mac
            sourceMac,
            // source mac
            Stack.MacAddress,
            // ether type
            (ushort)EtherType.IPv4,
            // payload
            ipv4Wrap.ToBytes() // wrapped in ethernet frame
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
