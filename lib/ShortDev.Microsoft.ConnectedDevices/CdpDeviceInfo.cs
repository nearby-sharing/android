using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using System.Text.Json.Serialization;

namespace ShortDev.Microsoft.ConnectedDevices;

public sealed record CdpDeviceInfo
{
    public required byte[] DeduplicationHint { get; init; }

    [JsonPropertyName("connectionModes")]
    public required ConnectionMode ConnectionModes { get; init; }

    [JsonPropertyName("deviceId")]
    public required byte[] DeviceId { get; init; }

    [JsonPropertyName("endpoints")]
    public required IReadOnlyList<EndpointInfo> Endpoints { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("type")]
    public required DeviceType Type { get; init; }
}