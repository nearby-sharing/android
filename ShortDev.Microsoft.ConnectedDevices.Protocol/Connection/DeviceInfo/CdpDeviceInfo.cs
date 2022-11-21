using System.Text.Json.Serialization;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.DeviceInfo;

public sealed class CdpDeviceInfo
{
    public byte[]? DeduplicationHint { get; set; }

    [JsonPropertyName("connectionModes")]
    public long ConnectionModes { get; set; }

    [JsonPropertyName("deviceId")]
    public byte[]? DeviceId { get; set; }

    [JsonPropertyName("endpoints")]
    public Endpoint[]? Endpoints { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public DeviceType Type { get; set; }
}

public sealed class Endpoint
{
    [JsonPropertyName("endpointType")]
    public long EndpointType { get; set; }

    [JsonPropertyName("host")]
    public string? Host { get; set; }

    [JsonPropertyName("service")]
    public string? Service { get; set; }
}
