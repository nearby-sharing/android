namespace ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;

public sealed class AdvertiseOptions
{
    public required int ManufacturerId { get; set; }
    public required BLeBeacon BeaconData { get; set; }
}
