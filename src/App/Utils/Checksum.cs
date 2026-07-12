using System.Buffers.Binary;

namespace App.Utils;

public static class Checksum
{
    // comments generate by AI and left to me to implement. AI also added the tests.
    // this function calculates the Internet checksum of a byte array. 
    // // The Internet checksum is defined in RFC 1071 and is used in various network protocols, including IP, TCP, and UDP. 
    // // The checksum is calculated by treating the input data as a sequence of 16-bit words, summing them up, and then taking the one's complement of the sum.
    public static ushort Calculate(byte[] bytes)
    {
        // Keep the running sum wider than 16 bits so carries are not lost.
        uint sum = 0;
        uint i = 0;

        // Read each complete pair of bytes as one big-endian 16-bit word.
        // Add each word to the running sum and advance by two bytes.
        while (i < bytes.Length - 1)
        {
            sum += BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan((int)i, 2));
            i += 2;
        }

        // If one byte remains, treat it as the high byte of a 16-bit word.
        // The missing low byte is zero for checksum purposes.
        if (i < bytes.Length)
        {
            sum += (uint)(bytes[i] << 8);
        }

        // Fold every carry from above 16 bits back into the low 16 bits.
        // Continue folding until no carry remains.
        // Hint: the loop condition should check whether sum has any bits above
        // the lowest 16 bits. In the loop body, separate the low 16 bits from
        // the carry above them, then add that carry back into the low portion.
        while ((sum >> 16) > 0)
        {
            ushort lowbits = (ushort)(sum << 16 >> 16);
            ushort highbits = (ushort)(sum >> 16);
            sum = (uint)(lowbits + highbits);
        }

        // Return the one's complement of the folded 16-bit sum.
        return (ushort)~sum;
    }
}
