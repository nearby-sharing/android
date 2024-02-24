using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;

public sealed class AdvertiseOptions
{
    public required int ManufacturerId { get; set; }
    public required BLeBeacon BeaconData { get; set; }
}
