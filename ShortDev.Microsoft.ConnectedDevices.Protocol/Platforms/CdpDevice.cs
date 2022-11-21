namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;

public abstract class CdpDevice
{
    public string? Name { get; init; }
    public string? Address { get; init; }
    public string? Alias { get; init; }
}
