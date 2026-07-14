using ShortDev.IO.Bond;
using System.Text;

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
    public T? Get<T>(string key)
        => Entries[key].Get<T>();

    public void Add(string key, byte value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_UInt8, UInt8Value = value });

    public void Add(string key, short value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Int16, Int16Value = value });

    public void Add(string key, ushort value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_UInt16, UInt16Value = value });

    public void Add(string key, int value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Int32, Int32Value = value });

    public void Add(string key, uint value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_UInt32, UInt32Value = value });

    public void Add(string key, long value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Int64, Int64Value = value });

    public void Add(string key, ulong value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_UInt64, UInt64Value = value });

    public void Add(string key, float value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Float, FloatValue = value });

    public void Add(string key, double value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Double, DoubleValue = value });

    public void Add(string key, char value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Char, CharValue = (ushort)value });

    public void Add(string key, bool value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Boolean, BooleanValue = value });

    public void Add(string key, string value, Encoding encoding)
    {
        if (encoding == Encoding.UTF8)
            Add(key, new PropertyValue() { Type = PropertyType.PropertyType_String, StringValue = value ?? string.Empty });
        else if (encoding == Encoding.Unicode)
            Add(key, new PropertyValue() { Type = PropertyType.PropertyType_WString, WStringValue = value ?? string.Empty });
        else
            throw new ArgumentOutOfRangeException(nameof(encoding));
    }

    public void Add(string key, DateTimeOffset value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_DateTimeOffset, DateTimeOffsetValue = value.ToFileTime() });

    public void Add(string key, TimeSpan value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_TimeSpan, TimeSpanValue = value.Ticks });

    public void Add(string key, Guid value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Guid, GuidValue = UUID.FromGuid(value) });

    public void Add(string key, Point value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Point, PointValue = value });

    public void Add(string key, Size value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Size, SizeValue = value });

    public void Add(string key, Rect value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Rect, RectValue = value });

    public void Add(string key, ValueSet value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_ValueSet, ValueSetValue = value });


    public void Add(string key, params List<byte> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_UInt8Array, UInt8ArrayValue = value });

    public void Add(string key, params List<short> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Int16Array, Int16ArrayValue = value });

    public void Add(string key, params List<ushort> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_UInt16Array, UInt16ArrayValue = value });

    public void Add(string key, params List<int> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Int32Array, Int32ArrayValue = value });

    public void Add(string key, params List<uint> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_UInt32Array, UInt32ArrayValue = value });

    public void Add(string key, params List<long> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_Int64Array, Int64ArrayValue = value });

    public void Add(string key, params List<ulong> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_UInt64Array, UInt64ArrayValue = value });

    public void Add(string key, params List<float> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_FloatArray, FloatArrayValue = value });

    public void Add(string key, params List<double> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_DoubleArray, DoubleArrayValue = value });

    public void Add(string key, params List<char> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_CharArray, CharArrayValue = value.ConvertAll(static c => (ushort)c) });

    public void Add(string key, params List<bool> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_BooleanArray, BooleanArrayValue = value.ConvertAll(static b => (byte)(b ? 1 : 0)) });

    public void Add(string key, Encoding encoding, params List<string> value)
    {
        if (encoding == Encoding.UTF8)
            Add(key, new PropertyValue() { Type = PropertyType.PropertyType_StringArray, StringArrayValue = value });
        else if (encoding == Encoding.Unicode)
            Add(key, new PropertyValue() { Type = PropertyType.PropertyType_WStringArray, WStringArrayValue = value });
        else
            throw new ArgumentOutOfRangeException(nameof(encoding));
    }

    public void Add(string key, params List<DateTimeOffset> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_DateTimeOffsetArray, DateTimeOffsetArrayValue = value.ConvertAll(static x => x.ToFileTime()) });

    public void Add(string key, params List<TimeSpan> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_TimeSpanArray, TimeSpanArrayValue = value.ConvertAll(static x => x.Ticks) });

    public void Add(string key, params List<Guid> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_GuidArray, GuidArrayValue = value.ConvertAll(UUID.FromGuid) });

    public void Add(string key, params List<Point> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_PointArray, PointArrayValue = value });

    public void Add(string key, params List<Size> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_SizeArray, SizeArrayValue = value });

    public void Add(string key, params List<Rect> value)
        => Add(key, new PropertyValue() { Type = PropertyType.PropertyType_RectArray, RectArrayValue = value });


}