using System.Buffers.Binary;
using App.Network.Ethernet;

namespace App.Network.Arp;

// an ARP packet has fields:
// - HardwareType (2 bytes)
// - ProtocolType (2 bytes)
// - HardwareSize (1 byte)
// - ProtocolSize (1 byte)
// - Opcode (2 bytes)
// - SenderMacAddress (6 bytes)
// - SenderIpAddress (4 bytes)
// - TargetMacAddress (6 bytes)
// - TargetIpAddress (4 bytes)
public sealed record ArpPacket(ushort HardwareType,
                               ushort ProtocolType,
                               byte HardwareSize,
                               byte ProtocolSize,
                               ushort Opcode,
                               MacAddress SenderMacAddress,
                               Ipv4Address SenderIpAddress,
                               MacAddress TargetMacAddress,
                               Ipv4Address TargetIpAddress) : IPacket<ArpPacket>
{
    public static ArpPacket Parse(byte[] payload)
    {
        ushort hardwareType = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(0, 2));
        ushort protocolType = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(2, 2));
        byte hardwareSize = payload[4];
        byte protocolSize = payload[5];
        ushort opcode = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(6, 2));
        MacAddress senderMacAddress = new MacAddress(payload[8], payload[9], payload[10], payload[11], payload[12], payload[13]);
        Ipv4Address senderIpAddress = new Ipv4Address($"{payload[14]}.{payload[15]}.{payload[16]}.{payload[17]}");
        MacAddress targetMacAddress = new MacAddress(payload[18], payload[19], payload[20], payload[21], payload[22], payload[23]);
        Ipv4Address targetIpAddress = new Ipv4Address($"{payload[24]}.{payload[25]}.{payload[26]}.{payload[27]}");

        return new ArpPacket(
            hardwareType,
            protocolType,
            hardwareSize,
            protocolSize,
            opcode,
            senderMacAddress,
            senderIpAddress,
            targetMacAddress,
            targetIpAddress
        );
    }

    public byte[] ToBytes()
    {
        byte[] bytes = new byte[28];
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(0, 2), HardwareType);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2, 2), ProtocolType);
        bytes[4] = HardwareSize;
        bytes[5] = ProtocolSize;
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(6, 2), Opcode);
        SenderMacAddress.CopyTo(bytes.AsSpan(8, 6));
        var senderIpArray = SenderIpAddress.ToArray();
        bytes[14] = senderIpArray[0];
        bytes[15] = senderIpArray[1];
        bytes[16] = senderIpArray[2];
        bytes[17] = senderIpArray[3];
        TargetMacAddress.CopyTo(bytes.AsSpan(18, 6));
        var targetIpArray = TargetIpAddress.ToArray();
        bytes[24] = targetIpArray[0];
        bytes[25] = targetIpArray[1];
        bytes[26] = targetIpArray[2];
        bytes[27] = targetIpArray[3];
        return bytes;
    }
}
