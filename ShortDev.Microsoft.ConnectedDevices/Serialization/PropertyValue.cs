using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ShortDev.Microsoft.ConnectedDevices.Serialization;

[DebuggerDisplay("Type = {Type}, Value = {Get()}")]
public partial class PropertyValue
{
    internal object Get()
    {
        return Type switch
        {
            PropertyType.PropertyType_Empty => throw new NullReferenceException(),
            PropertyType.PropertyType_UInt8Array => UInt8ArrayValue,
            PropertyType.PropertyType_Int32 => Int32Value,
            PropertyType.PropertyType_UInt32 => UInt32Value,
            PropertyType.PropertyType_UInt32Array => UInt32ArrayValue,
            PropertyType.PropertyType_UInt64 => UInt64Value,
            PropertyType.PropertyType_UInt64Array => UInt64ArrayValue,
            PropertyType.PropertyType_String => StringValue,
            PropertyType.PropertyType_StringArray => StringArrayValue,
            PropertyType.PropertyType_GuidArray => GuidArrayValue,
            _ => throw new NotImplementedException(),
        };
    }

    public T Get<T>()
    {
        var value = Get();
        if (typeof(T) == typeof(Guid))
            return (T)(object)((List<UUID>)value)[0].ToGuid();

        return (T)value;
    }

    public static PropertyValue Create<T>(T value)
    {
        PropertyValue result = new();
        result.Set(value);
        return result;
    }

    public void Set<T>(T value)
    {
        if (value is Guid guidValue)
            SetInternal(new[] { guidValue });
        else
            SetInternal(value);
    }

    void SetInternal<T>(T value)
    {
        switch (value)
        {
            case IEnumerable<byte> uint8ArrayValue:
                Type = PropertyType.PropertyType_UInt8Array;
                this.UInt8ArrayValue = uint8ArrayValue.ToList();
                break;
            case int int32Value:
                Type = PropertyType.PropertyType_Int32;
                this.Int32Value = int32Value;
                break;
            case uint uint32Value:
                Type = PropertyType.PropertyType_UInt32;
                this.UInt32Value = uint32Value;
                break;
            case IEnumerable<uint> uint32ArrayValue:
                Type = PropertyType.PropertyType_UInt32Array;
                this.UInt32ArrayValue = uint32ArrayValue.ToList();
                break;
            case ulong uint64Value:
                Type = PropertyType.PropertyType_UInt64;
                this.UInt64Value = uint64Value;
                break;
            case IEnumerable<ulong> uint64ArrayValue:
                Type = PropertyType.PropertyType_UInt64Array;
                this.UInt64ArrayValue = uint64ArrayValue.ToList();
                break;
            case string stringValue:
                Type = PropertyType.PropertyType_String;
                this.StringValue = stringValue;
                break;
            case IEnumerable<string> stringArrayValue:
                Type = PropertyType.PropertyType_StringArray;
                this.StringArrayValue = stringArrayValue.ToList();
                break;
            case IEnumerable<Guid> guidArrayValue:
                Type = PropertyType.PropertyType_GuidArray;
                this.GuidArrayValue = guidArrayValue.Select(UUID.FromGuid).ToList();
                break;
            default:
                throw new NotImplementedException();
        }
    }
}
