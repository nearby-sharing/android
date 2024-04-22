namespace ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;

public sealed class ScanOptions
{
    public DeviceDiscovered? OnDeviceDiscovered { get; set; }

    public delegate void DeviceDiscovered(BLeBeacon beacon, double rssi = double.NegativeInfinity);
}
