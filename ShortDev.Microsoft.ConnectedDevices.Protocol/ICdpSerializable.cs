using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

public interface ICdpSerializable<T> where T : ICdpSerializable<T>
{
    static abstract T Parse(BinaryReader reader);
    public static bool TryParse(BinaryReader reader, out T? result, out Exception? error)
        => throw new NotImplementedException();

    void Write(BinaryWriter writer);

    public long CalcSize()
    {
        using (MemoryStream stream = new())
        using (BinaryWriter writer = new(stream))
        {
            Write(writer);
            return stream.Length;
        }
    }
}
