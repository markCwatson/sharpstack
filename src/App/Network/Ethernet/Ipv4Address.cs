using System.Globalization;

namespace App.Network.Ethernet;

public record struct Ipv4Address(string Value)
{
    public byte[] ToArray()
    {
        var parts = Value.Split('.');
        if (parts.Length != 4)
            throw new FormatException("Invalid IP address format.");

        return new byte[]
        {
            byte.Parse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture),
            byte.Parse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture),
            byte.Parse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture),
            byte.Parse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture)
        };
    }
}
