using ShortDev.IO.Bond;

namespace ShortDev.Microsoft.ConnectedDevices.Serialization;

public partial class ValueSet : IBinaryWritable<ValueSet>
{
    // The Bond serialization does not work with the size calculator
    ulong IBinaryWritable<ValueSet>.MinimumSize { get; } = 0;

    public static ValueSet Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        CompactBinaryReader<TReader> bondReader = new(ref reader);
        return (ValueSet)ValueSetHelper.Deserialize(ref bondReader);
    }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        CompactBinaryWriter<TWriter> bondWriter = new(ref writer);
        ValueSetHelper.Serialize(this, ref bondWriter);
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