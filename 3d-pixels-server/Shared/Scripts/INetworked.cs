using ByteStream.Interfaces;

public interface INetworked
{
    void Serialize(IByteStream stream);
}

public static class Hasher
{
    public static int GetFNV1aHashCode(string str)
    {
        if (str == null)
            return 0;
        var length = str.Length;
        // original FNV-1a has 32 bit offset_basis = 2166136261 but length gives a bit better dispersion (2%) for our case where all the strings are equal length, for example: "3EC0FFFF01ECD9C4001B01E2A707"
        int hash = length;
        for (int i = 0; i != length; ++i)
            hash = (hash ^ str[i]) * 16777619;
        return hash;
    }
}