using Bond;
using System;

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

    public T Get<T>(T value)
    {
        switch (value)
        {
            case UInt32 uint32Value:
                this.UInt32Value = uint32Value;
                break;
            case string stringValue:
                this.StringValue = stringValue;
                break;
            case Guid guidArrayValue:
                this.GuidArrayValue = new() { UUID.FromGuid(guidArrayValue) };
                break;
        }
        throw new NotImplementedException();
    }
}
