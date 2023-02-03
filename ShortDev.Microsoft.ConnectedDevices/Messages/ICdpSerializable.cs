﻿using ShortDev.Networking;
using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

public interface ICdpSerializable<T> : ICdpWriteable where T : ICdpSerializable<T>
{
    static abstract T Parse(BinaryReader reader);
    public static bool TryParse(BinaryReader reader, out T? result, out Exception? error)
        => throw new NotImplementedException();

    public long CalcSize()
    {
        EndianWriter writer = new(Endianness.BigEndian);
        Write(writer);
        return writer.Buffer.Size;
    }
}

public interface ICdpArraySerializable<T> where T : ICdpArraySerializable<T>
{
    static abstract T[] ParseArray(BinaryReader reader);
    static abstract void WriteArray(EndianWriter writer, T[] array);
}

public interface ICdpWriteable
{
    void Write(EndianWriter writer);
}
