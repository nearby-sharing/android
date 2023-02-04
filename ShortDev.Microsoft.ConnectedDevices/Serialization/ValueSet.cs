using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Networking;

namespace ShortDev.Microsoft.ConnectedDevices.Serialization;

public partial class ValueSet : ICdpPayload<ValueSet>
{
    public static ValueSet Parse(EndianReader reader)
    {
        // ToDo: We really should not re-allocated here!!
        var data = reader.ReadToEnd().ToArray();
        CompactBinaryReader<InputBuffer> bondReader = new(new(data));
        return Deserialize<ValueSet>.From(bondReader);
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