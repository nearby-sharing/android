using System.Text.Json.Serialization;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;

[JsonSerializable(typeof(CdpDeviceInfo), GenerationMode = JsonSourceGenerationMode.Serialization)]
internal sealed partial class DeviceInfoJsonContext : JsonSerializerContext;
