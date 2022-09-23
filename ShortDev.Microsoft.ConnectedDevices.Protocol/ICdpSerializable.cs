using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

public interface ICdpSerializable<T> where T : ICdpSerializable<T>
{
    static abstract T Parse(BinaryReader reader);
    static bool TryParse(BinaryReader reader, out T? result, out Exception? error)
        => throw new NotImplementedException();

    void Write(BinaryWriter writer);
}
