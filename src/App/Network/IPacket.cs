namespace App.Network;

public interface IPacket<T>
{
    public abstract static T Parse(byte[] bytes);
    public abstract byte[] ToBytes();
}
