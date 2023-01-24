namespace ShortDev.Microsoft.ConnectedDevices.Platforms;

public record CdpDevice(string Name, string Address)
{
    public string? MacAddress { get; init; }
}
