using System.Buffers.Binary;

namespace App.Network.Ethernet;

//// ethernet header is 14 bytes
// destination mac: 6 bytes
// source mac: 6 bytes
// ether type: 2 bytes
// payload: variable length

public record struct EthernetFrame(MacAddress Destination,
                                   MacAddress Source,
                                   ushort EtherType,
                                   byte[] Payload) : IPacket<EthernetFrame>
{
    public EtherType EtherTypeEnum => (EtherType)EtherType;

    public static EthernetFrame Parse(byte[] bytes)
    {
        if (bytes.Length < 14)
            throw new ArgumentException("Ethernet frame must be at least 14 bytes long.");

        var destination = new MacAddress(bytes[0], bytes[1], bytes[2], bytes[3], bytes[4], bytes[5]);
        var source = new MacAddress(bytes[6], bytes[7], bytes[8], bytes[9], bytes[10], bytes[11]);
        ushort etherType = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(12, 2));
        var payload = bytes[14..];

        return new EthernetFrame(destination, source, etherType, payload);
    }

    public byte[] ToBytes()
    {
        var bytes = new byte[14 + Payload.Length];
        Destination.CopyTo(bytes.AsSpan(0, 6));
        Source.CopyTo(bytes.AsSpan(6, 6));
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(12, 2), EtherType);
        Payload.CopyTo(bytes.AsSpan(14));
        return bytes;
    }
}
