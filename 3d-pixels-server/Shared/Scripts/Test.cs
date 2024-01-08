using System.Text;
using ByteStream.Interfaces;

public static class Test
{
    public static void Speak()
    {
        Logger.Log("Hello World");
    }
}

public struct MessageA : INetworked
{
    public int value;

    public void Serialize(IByteStream stream)
    {
        stream.Serialize(ref value);
    }
}

public struct MessageB : INetworked
{
    public string value;

    public void Serialize(IByteStream stream)
    {
        stream.SerializeString(ref value, Encoding.ASCII);
    }
}