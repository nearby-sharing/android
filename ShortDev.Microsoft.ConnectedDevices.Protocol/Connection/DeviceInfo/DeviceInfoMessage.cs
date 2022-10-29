using ShortDev.Networking;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.DeviceInfo;

public sealed class DeviceInfoMessage : ICdpPayload<DeviceInfoMessage>
{
    public static DeviceInfoMessage Parse(BinaryReader reader)
        => new()
        {
            JsonData = reader.ReadStringWithLength()
        };

    public required string JsonData { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteWithLength(JsonData);
    }
}
