using System.Buffers.Binary;
using App.Network.Ethernet;
using App.Network.ICMP;

namespace App.Network.IPv4;

// An IPv4 packet consists of a 20 to 60-byte header followed by a data payload:
// version (4 bits)
// header length (4 bits)
// type of service (8 bits)
// total length (16 bits)
// identification (16 bits)
// flags (3 bits)
// fragment offset (13 bits)
// time to live (8 bits)
// protocol (8 bits)
// header checksum (16 bits)
// source IP address (32 bits)
// destination IP address (32 bits)
public sealed record IPv4Packet(byte Version,
                                byte HeaderLength,
                                byte TypeOfService,
                                ushort TotalLength,
                                ushort Identification,
                                byte Flags,
                                ushort FragmentOffset,
                                byte TimeToLive,
                                byte Protocol,
                                ushort HeaderChecksum,
                                Ipv4Address Source,
                                Ipv4Address Destination,
                                byte[] Payload)
{

    public Ipv4Protocol ProtocolEnum => (Ipv4Protocol)Protocol;

    public static IPv4Packet Parse(byte[] bytes)
    {
        if (bytes.Length < 20)
            throw new ArgumentException("IPv4 packet must be at least 20 bytes long.");

        byte version = (byte)(bytes[0] >> 4);
        byte headerLength = (byte)(bytes[0] & 0x0F);
        byte typeOfService = bytes[1];
        ushort totalLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(2, 2));
        ushort identification = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(4, 2));
        byte flags = (byte)(bytes[6] >> 5);
        ushort fragmentOffset = (ushort)(BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(6, 2)) & 0x1FFF);
        byte timeToLive = bytes[8];
        byte protocol = bytes[9];
        ushort headerChecksum = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(10, 2));
        Ipv4Address source = new Ipv4Address($"{bytes[12]}.{bytes[13]}.{bytes[14]}.{bytes[15]}");
        Ipv4Address destination = new Ipv4Address($"{bytes[16]}.{bytes[17]}.{bytes[18]}.{bytes[19]}");
        // why x4? because headerlength is the count of 32-bit words, so multiply it by 4 to get bytes
        // eg: header length of 5 means 5 32-bit words, so 5 * 4 = 20 bytes
        byte[] payload = bytes[(headerLength * 4)..totalLength];

        return new IPv4Packet(
            version,
            headerLength,
            typeOfService,
            totalLength,
            identification,
            flags,
            fragmentOffset,
            timeToLive,
            protocol,
            headerChecksum,
            source,
            destination,
            payload
        );
    }

    public static async Task<EthernetFrame?> HandlePacket(EthernetFrame incoming)
    {
        IPv4Packet packet = Parse(incoming.Payload);

        if (packet.Destination != Stack.Ipv4Address)
            return null;

        return packet.ProtocolEnum switch
        {
            Ipv4Protocol.ICMP => await IcmpPacket.HandlePacket(packet),
            //Ipv4Protocol.TCP => await TcpPacket.HandlePacket(packet),
            _ => null
        };
    }
}
