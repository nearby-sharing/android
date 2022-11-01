using Bond;
using System;
using System.ComponentModel.DataAnnotations;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Serialization;

public partial class PropertyValue
{
    public BondDataType BondType
        => (BondDataType)Type;

    public PropertyType WindowsType
        => (PropertyType)Type;

    public T Get<T>()
    {
        switch (System.Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.UInt32:
                return (T)(object)UInt32Value;
            case TypeCode.String:
                return (T)(object)StringValue;
        }
        if (typeof(T) == typeof(Guid))
            return (T)(object)GuidArrayValue[0].ToGuid();

        throw new NotImplementedException();
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
            case UInt32 uint32Value:
                Type = (int)PropertyType.PropertyType_UInt32;
                this.UInt32Value = uint32Value;
                break;
            //case string stringValue:
            //    this.StringValue = stringValue;
            //    break;
            //case Guid guidArrayValue:
            //    this.GuidArrayValue = new() { UUID.FromGuid(guidArrayValue) };
            //    break;
            default:
                throw new NotImplementedException();
        }
    }
}
