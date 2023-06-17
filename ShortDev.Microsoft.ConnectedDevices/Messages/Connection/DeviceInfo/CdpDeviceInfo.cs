using System.Collections.Generic;
using System.Text.Json.Serialization;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;

public sealed class CdpDeviceInfo
{
    public byte[]? DeduplicationHint { get; set; }

    [JsonPropertyName("connectionModes")]
    public ConnectionMode ConnectionModes { get; set; }

    [JsonPropertyName("deviceId")]
    public byte[]? DeviceId { get; set; }

    [JsonPropertyName("endpoints")]
    public IReadOnlyList<EndpointInfo>? Endpoints { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public DeviceType Type { get; set; }
}