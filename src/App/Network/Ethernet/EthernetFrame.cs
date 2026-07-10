namespace App.Network.Ethernet;

//// ethernet header is 14 bytes
// destination mac: 6 bytes
// source mac: 6 bytes
// ether type: 2 bytes
// payload: variable length

public record struct EthernetFrame(MacAddress Destination, MacAddress Source, ushort EtherType, byte[] Payload)
{
    public EtherType EtherTypeEnum => (EtherType)EtherType;
}
