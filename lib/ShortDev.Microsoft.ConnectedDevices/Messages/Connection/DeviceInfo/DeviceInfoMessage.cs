using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using System.Text.Json;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;

/// <summary>
/// This message requests information from the device.
/// </summary>
public sealed class DeviceInfoMessage : IBinaryWritable, IBinaryParsable<DeviceInfoMessage>
{
    public static DeviceInfoMessage Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            DeviceInfo = JsonSerializer.Deserialize<CdpDeviceInfo>(reader.ReadStringWithLength()) ?? throw new CdpProtocolException("Invalid device info")
        };

    /// <summary>
    /// A variable length payload to specify information about the source device.
    /// </summary>
    public required CdpDeviceInfo DeviceInfo { get; init; }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.WriteWithLength(JsonSerializer.Serialize(DeviceInfo));
    }
}
