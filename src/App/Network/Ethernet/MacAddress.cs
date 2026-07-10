namespace App.Network.Ethernet;

public record struct MacAddress(byte Byte1, byte Byte2, byte Byte3, byte Byte4, byte Byte5, byte Byte6)
{
    public void CopyTo(Span<byte> destination)
    {
        if (destination.Length < 6)
            throw new ArgumentException("Destination span must be at least 6 bytes long.");

        destination[0] = Byte1;
        destination[1] = Byte2;
        destination[2] = Byte3;
        destination[3] = Byte4;
        destination[4] = Byte5;
        destination[5] = Byte6;
    }
}
