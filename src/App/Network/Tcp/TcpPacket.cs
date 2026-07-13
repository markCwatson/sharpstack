using App.Network.Ethernet;
using App.Network.IPv4;
using System.Buffers.Binary;

namespace App.Network.Tcp;

// a tcp packet contains: (20 bytes header + payload)
// Source port - 2 bytes
// Destination port - 2 bytes
// Sequence number - 4 bytes
// Acknowledgment number - 4 bytes
// Flags - 2 bytes (URG, ACK, PSH, RST, SYN, FIN)
// Window size - 2 bytes
// Checksum - 2 bytes
// Urgent pointer - 2 bytes
// TCP payload
public sealed record TcpPacket(ushort Port, ushort DestinationPort, uint SequenceNumber, uint AcknowledgmentNumber, ushort Flags, ushort WindowSize, ushort Checksum, ushort UrgentPointer, byte[] Payload)
{
    public static TcpPacket Parse(byte[] bytes)
    {
        if (bytes.Length < 20)
            throw new ArgumentException("TCP packet must be at least 20 bytes long.");

        return new TcpPacket(
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(0, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(2, 2)),
            BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(4, 4)),
            BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(8, 4)),
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(12, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(14, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(16, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(18, 2)),
            bytes[20..]
        );
    }

    public static async Task<EthernetFrame?> HandlePacket(IPv4Packet packet, MacAddress sourceMac, MacAddress destinationMac)
    {
        // parse: TcpPacket

        // create a key based on the source ip and source port (or use a four-tuple?)

        // check if there is an existing connection for that key (a TcpConnection class)
        /// connection state machine? reorder packets?

        //  use connection to do what? pass to next layer like http ?
        //// we will use the Destination port to determine what to do with the packet
        //// let's use an interface listeners of IApplication 

        // get response from next layer, wrap in tcp packet, wrap in ipv4 packet, wrap in ethernet frame 

        // return ethernet frame
        return null;
    }
}
