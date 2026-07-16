using System.Buffers.Binary;
using App.Network.Ethernet;
using App.Network.IPv4;

namespace App.Network.Tcp;

// Adds TCP/IPv4 checksums and wraps an outbound TCP packet in IPv4 and Ethernet.
public static class TcpFrameEncoder
{
    public static EthernetFrame Encode(
        TcpPacket outbound,
        IPv4Packet incomingIpPacket,
        MacAddress remoteMac)
    {
        TcpPacket checksummedTcp = AddTcpChecksum(
            outbound,
            incomingIpPacket.Destination,
            incomingIpPacket.Source);

        var outgoingIpPacket = new IPv4Packet(
            incomingIpPacket.Version,
            incomingIpPacket.HeaderLength,
            incomingIpPacket.TypeOfService,
            (ushort)(incomingIpPacket.HeaderLength * 4 + checksummedTcp.ToBytes().Length),
            incomingIpPacket.Identification,
            incomingIpPacket.Flags,
            incomingIpPacket.FragmentOffset,
            incomingIpPacket.TimeToLive,
            incomingIpPacket.Protocol,
            0,
            incomingIpPacket.Destination,
            incomingIpPacket.Source,
            checksummedTcp.ToBytes());

        byte[] outgoingIpBytes = outgoingIpPacket.ToBytes();
        int headerLength = outgoingIpPacket.HeaderLength * 4;
        ushort ipChecksum = Utils.Checksum.Calculate(outgoingIpBytes[..headerLength]);
        outgoingIpPacket = outgoingIpPacket with { HeaderChecksum = ipChecksum };

        return new EthernetFrame(
            remoteMac,
            Stack.MacAddress,
            (ushort)EtherType.IPv4,
            outgoingIpPacket.ToBytes());
    }

    private static TcpPacket AddTcpChecksum(
        TcpPacket packet,
        Ipv4Address source,
        Ipv4Address destination)
    {
        TcpPacket packetWithoutChecksum = packet with { Checksum = 0 };
        byte[] tcpBytes = packetWithoutChecksum.ToBytes();
        var pseudoHeader = new PseudoHeader(
            source,
            destination,
            0,
            (byte)Ipv4Protocol.TCP,
            (ushort)tcpBytes.Length);
        ushort checksum = Utils.Checksum.Calculate(pseudoHeader.ToBytes().Concat(tcpBytes).ToArray());
        return packetWithoutChecksum with { Checksum = checksum };
    }

    private sealed record PseudoHeader(
        Ipv4Address Source,
        Ipv4Address Destination,
        byte Zero,
        byte Protocol,
        ushort TcpLength)
    {
        public byte[] ToBytes()
        {
            byte[] bytes = new byte[12];
            Source.ToArray().CopyTo(bytes, 0);
            Destination.ToArray().CopyTo(bytes, 4);
            bytes[8] = Zero;
            bytes[9] = Protocol;
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(10, 2), TcpLength);
            return bytes;
        }
    }
}
