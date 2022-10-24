﻿using ShortDev.Networking;
using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

public interface ICdpSerializable<T> : ICdpWriteable where T : ICdpSerializable<T>
{
    static abstract T Parse(BinaryReader reader);
    public static bool TryParse(BinaryReader reader, out T? result, out Exception? error)
        => throw new NotImplementedException();

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

public interface ICdpWriteable
{
    void Write(BinaryWriter writer);

    public byte[] ToArray()
    {
        using (MemoryStream stream = new())
        using (BigEndianBinaryWriter writer = new(stream))
        {
            Write(writer);
            return stream.ToArray();
        }
    }
}
