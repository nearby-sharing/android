namespace ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;

public sealed class AdvertiseOptions
{
    public int ManufacturerId { get; set; }
    public byte[]? BeaconData { get; set; }
    public string GattServiceId { get; set; }
}
