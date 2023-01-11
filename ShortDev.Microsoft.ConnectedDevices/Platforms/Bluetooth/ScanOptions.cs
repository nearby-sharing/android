using System;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;

public sealed class ScanOptions<TDevice>
{
    public Action<TDevice>? OnDeviceDiscovered { get; set; }
}
