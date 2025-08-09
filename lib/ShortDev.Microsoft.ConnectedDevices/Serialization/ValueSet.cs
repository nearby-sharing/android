using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;

namespace ShortDev.Microsoft.ConnectedDevices.Serialization;

public partial class ValueSet : IBinaryWritable
{
    public static ValueSet Parse(ref HeapEndianReader reader)
    {
        // ToDo: We really should not re-allocated here!!
        byte[] data = new byte[reader.Stream.Length - reader.Stream.Position];
        reader.ReadBytes(data);
        CompactBinaryReader<InputBuffer> bondReader = new(new(data));
        return Deserialize<ValueSet>.From(bondReader);
    }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
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