using System.Text.Json.Serialization;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;

[JsonSerializable(typeof(CdpDeviceInfo))]
internal sealed partial class DeviceInfoJsonContext : JsonSerializerContext;
