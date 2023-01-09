namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;

public class CdpDevice
{
    public required string Name { get; init; }
    public required string Address { get; init; }
    public string? Alias { get; init; }
}
