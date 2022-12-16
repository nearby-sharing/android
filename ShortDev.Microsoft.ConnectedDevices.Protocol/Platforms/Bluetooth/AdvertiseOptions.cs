namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms.Bluetooth;

public sealed class AdvertiseOptions
{
    public int ManufacturerId { get; set; }
    public byte[]? BeaconData { get; set; }
}
