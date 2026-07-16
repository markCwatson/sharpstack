using App.Network.Ethernet;

namespace App.Network.Arp;

public static class ArpFrameEncoder
{
    public static EthernetFrame Encode(ArpPacket arpPacket, MacAddress sourceMac, Ipv4Address sourceIp)
    {
        ArpPacket response = new ArpPacket(
            HardwareType: arpPacket.HardwareType,
            ProtocolType: arpPacket.ProtocolType,
            HardwareSize: arpPacket.HardwareSize,
            ProtocolSize: arpPacket.ProtocolSize,
            Opcode: 2, // reply
            SenderMacAddress: sourceMac,
            SenderIpAddress: sourceIp,
            TargetMacAddress: arpPacket.SenderMacAddress,
            TargetIpAddress: arpPacket.SenderIpAddress
        );

        return new EthernetFrame(
            Destination: arpPacket.SenderMacAddress,
            Source: sourceMac,
            EtherType: (ushort)EtherType.ARP,
            Payload: response.ToBytes()
        );
    }
}
