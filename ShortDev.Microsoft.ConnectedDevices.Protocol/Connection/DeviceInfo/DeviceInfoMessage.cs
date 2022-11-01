using ShortDev.Networking;
using System.IO;
using System.Text.Json;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.DeviceInfo;

public sealed class DeviceInfoMessage : ICdpPayload<DeviceInfoMessage>
{
    public static DeviceInfoMessage Parse(BinaryReader reader)
        => new()
        {
            DeviceInfo = JsonSerializer.Deserialize<CdpDeviceInfo>(reader.ReadStringWithLength()) ?? throw new InvalidDataException()
        };

    public required CdpDeviceInfo DeviceInfo { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteWithLength(JsonSerializer.Serialize(DeviceInfo));
    }
}
