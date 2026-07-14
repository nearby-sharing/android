using System.Diagnostics;

namespace ShortDev.Microsoft.ConnectedDevices.Serialization;

[DebuggerDisplay("Type = {Type}, Value = {Get()}")]
public partial struct PropertyValue
{
    internal readonly object? Get()
    {
        return Type switch
        {
            PropertyType.PropertyType_Empty => null,
            PropertyType.PropertyType_UInt8 => UInt8Value,
            PropertyType.PropertyType_Int16 => Int16Value,
            PropertyType.PropertyType_UInt16 => UInt16Value,
            PropertyType.PropertyType_Int32 => Int32Value,
            PropertyType.PropertyType_UInt32 => UInt32Value,
            PropertyType.PropertyType_Int64 => Int64Value,
            PropertyType.PropertyType_UInt64 => UInt64Value,
            PropertyType.PropertyType_Float => FloatValue,
            PropertyType.PropertyType_Double => DoubleValue,
            PropertyType.PropertyType_Char => (char)CharValue,
            PropertyType.PropertyType_Boolean => BooleanValue,
            PropertyType.PropertyType_WString => WStringValue,
            PropertyType.PropertyType_DateTimeOffset => DateTimeOffset.FromFileTime(DateTimeOffsetValue),
            PropertyType.PropertyType_TimeSpan => TimeSpan.FromTicks(TimeSpanValue),
            PropertyType.PropertyType_Guid => GuidValue?.ToGuid() ?? default,
            PropertyType.PropertyType_Point => PointValue,
            PropertyType.PropertyType_Size => SizeValue,
            PropertyType.PropertyType_Rect => RectValue,
            PropertyType.PropertyType_ValueSet => ValueSetValue,

            PropertyType.PropertyType_String => StringValue,

            PropertyType.PropertyType_UInt8Array => UInt8ArrayValue,
            PropertyType.PropertyType_Int16Array => Int16ArrayValue,
            PropertyType.PropertyType_UInt16Array => UInt16ArrayValue,
            PropertyType.PropertyType_Int32Array => Int32ArrayValue,
            PropertyType.PropertyType_UInt32Array => UInt32ArrayValue,
            PropertyType.PropertyType_Int64Array => Int64ArrayValue,
            PropertyType.PropertyType_UInt64Array => UInt64ArrayValue,
            PropertyType.PropertyType_FloatArray => FloatArrayValue,
            PropertyType.PropertyType_DoubleArray => DoubleArrayValue,
            PropertyType.PropertyType_CharArray => CharArrayValue.ConvertAll(static x => unchecked((char)x)),
            PropertyType.PropertyType_BooleanArray => BooleanArrayValue.ConvertAll(static x => x != 0),
            PropertyType.PropertyType_WStringArray => WStringArrayValue,
            PropertyType.PropertyType_DateTimeOffsetArray => DateTimeOffsetArrayValue.ConvertAll(DateTimeOffset.FromFileTime),
            PropertyType.PropertyType_TimeSpanArray => TimeSpanArrayValue.ConvertAll(TimeSpan.FromTicks),
            PropertyType.PropertyType_GuidArray => GuidArrayValue.ConvertAll(static x => x.ToGuid()),
            PropertyType.PropertyType_PointArray => PointArrayValue,
            PropertyType.PropertyType_SizeArray => SizeArrayValue,
            PropertyType.PropertyType_RectArray => RectArrayValue,

            PropertyType.PropertyType_StringArray => StringArrayValue,
            _ => throw new UnreachableException("Invalid property type"),
        };
    }

    public readonly T? Get<T>()
        => (T?)Get();
}
