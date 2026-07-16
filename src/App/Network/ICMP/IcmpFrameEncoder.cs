using App.Network.Ethernet;
using App.Network.IPv4;

namespace App.Network.ICMP;

public static class IcmpFrameEncoder
{
    public static EthernetFrame Encode(IcmpPacket icmpPacket, IPv4Packet ipv4Packet, MacAddress sourceMac, MacAddress destinationMac)
    {
        IcmpPacket reply = new IcmpPacket(
            (byte)IcmpType.EchoReply,
            (byte)IcmpCode.EchoReply,
            0, // checksum
            icmpPacket.Identifier,
            icmpPacket.SequenceNumber,
            icmpPacket.Payload
        );

        byte[] replyInBytes = reply.ToBytes();
        ushort replyChecksum = Utils.Checksum.Calculate(replyInBytes);
        reply = reply with { Checksum = replyChecksum };

        IPv4Packet ipv4Wrap = new IPv4Packet(
            ipv4Packet.Version,
            ipv4Packet.HeaderLength,
            ipv4Packet.TypeOfService,
            (ushort)(ipv4Packet.HeaderLength * 4 + reply.ToBytes().Length),
            ipv4Packet.Identification,
            ipv4Packet.Flags,
            ipv4Packet.FragmentOffset,
            ipv4Packet.TimeToLive,
            ipv4Packet.Protocol,
            0, // checksum
            ipv4Packet.Destination,
            ipv4Packet.Source,
            reply.ToBytes() // wrapped in IPv4 packet
        );

        byte[] inBytes = ipv4Wrap.ToBytes();
        ushort ipv4WrapChecksum = Utils.Checksum.Calculate(inBytes);
        ipv4Wrap = ipv4Wrap with { HeaderChecksum = ipv4WrapChecksum };

        return new EthernetFrame(
            sourceMac,
            destinationMac,
            (ushort)EtherType.IPv4,
            ipv4Wrap.ToBytes() // wrapped in ethernet frame
        );
    }
}
