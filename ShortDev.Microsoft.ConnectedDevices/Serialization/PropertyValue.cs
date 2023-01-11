using System;
using System.Collections.Generic;
using System.Linq;

namespace ShortDev.Microsoft.ConnectedDevices.Serialization;

public partial class PropertyValue
{
    internal object Get()
    {
        switch (Type)
        {
            case PropertyType.PropertyType_Empty:
                throw new NullReferenceException();
            case PropertyType.PropertyType_UInt8Array:
                return UInt8ArrayValue;
            case PropertyType.PropertyType_UInt32:
                return UInt32Value;
            case PropertyType.PropertyType_UInt32Array:
                return UInt32ArrayValue;
            case PropertyType.PropertyType_UInt64:
                return UInt64Value;
            case PropertyType.PropertyType_UInt64Array:
                return UInt64ArrayValue;
            case PropertyType.PropertyType_String:
                return StringValue;
            case PropertyType.PropertyType_StringArray:
                return StringArrayValue;
            case PropertyType.PropertyType_GuidArray:
                return GuidArrayValue;
        }
        throw new NotImplementedException();
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
        switch (value)
        {
            case IEnumerable<byte> uint8ArrayValue:
                Type = PropertyType.PropertyType_UInt8Array;
                // this.UInt8ArrayValue = uint8ArrayValue.ToList();
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

    public override string ToString()
        => $"{{ Type = {Type}, Value = {Get()} }}";
}
