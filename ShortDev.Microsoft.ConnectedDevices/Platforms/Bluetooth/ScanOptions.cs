using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;

public sealed class ScanOptions
{
    public DeviceDiscovered? OnDeviceDiscovered { get; set; }

    public delegate void DeviceDiscovered(BLeBeacon beacon, double rssi = double.NegativeInfinity);
}
