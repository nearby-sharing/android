﻿using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Networking;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Serialization;

public partial class ValueSet : ICdpPayload<ValueSet>
{
    public static ValueSet Parse(byte[] data)
    {
        using MemoryStream stream = new(data);
        return Parse(stream);
    }

    public static ValueSet Parse(BinaryReader reader)
        => Parse(reader.BaseStream);

    public static ValueSet Parse(Stream stream)
    {
        CompactBinaryReader<InputStream> reader = new(new(stream));
        return Deserialize<ValueSet>.From(reader);
    }

    public void Write(EndianWriter writer)
    {
        OutputBuffer buffer = new();
        CompactBinaryWriter<OutputBuffer> bondWriter = new(buffer);
        Serialize.To(bondWriter, this);
        writer.Write(buffer.Data);
    }

    /// <summary>
    /// Get's the value of the specified key <br/>
    /// Throws if the key is not found
    /// </summary>
    public T Get<T>(string key)
        => Entries[key].Get<T>();

    public void Add<T>(string key, T value)
        => Entries.Add(key, PropertyValue.Create(value));

    public bool ContainsKey(string key)
        => Entries.ContainsKey(key);
}