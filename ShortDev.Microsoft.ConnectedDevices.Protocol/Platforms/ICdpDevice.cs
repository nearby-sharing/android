namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;

public interface ICdpDevice
{
    public string? Name { get; init; }
    string? Address { get; }
}
