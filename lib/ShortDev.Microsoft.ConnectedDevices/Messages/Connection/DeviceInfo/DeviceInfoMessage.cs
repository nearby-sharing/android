using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using System.Text.Json;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;

/// <summary>
/// This message requests information from the device.
/// </summary>
public sealed class DeviceInfoMessage : ICdpPayload<DeviceInfoMessage>
{
    public static DeviceInfoMessage Parse(ref EndianReader reader)
        => new()
        {
            DeviceInfo = JsonSerializer.Deserialize(
                reader.ReadStringWithLength(),
                DeviceInfoJsonContext.Default.CdpDeviceInfo
            ) ?? throw new CdpProtocolException("Invalid device info")
        };

    /// <summary>
    /// A variable length payload to specify information about the source device.
    /// </summary>
    public required CdpDeviceInfo DeviceInfo { get; init; }

    public void Write(EndianWriter writer)
    {
        writer.WriteWithLength(
            JsonSerializer.Serialize(
                DeviceInfo,
                DeviceInfoJsonContext.Default.CdpDeviceInfo
            )
        );
    }
}
