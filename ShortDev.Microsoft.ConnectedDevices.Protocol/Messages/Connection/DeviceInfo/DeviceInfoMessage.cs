using ShortDev.Microsoft.ConnectedDevices.Protocol.Exceptions;
using ShortDev.Networking;
using System.IO;
using System.Text.Json;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Messages.Connection.DeviceInfo;

/// <summary>
/// This message requests information from the device.
/// </summary>
public sealed class DeviceInfoMessage : ICdpPayload<DeviceInfoMessage>
{
    public static DeviceInfoMessage Parse(BinaryReader reader)
        => new()
        {
            DeviceInfo = JsonSerializer.Deserialize<CdpDeviceInfo>(reader.ReadStringWithLength()) ?? throw new CdpProtocolException("Invalid device info")
        };

    /// <summary>
    /// A variable length payload to specify information about the source device.
    /// </summary>
    public required CdpDeviceInfo DeviceInfo { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteWithLength(JsonSerializer.Serialize(DeviceInfo));
    }
}
