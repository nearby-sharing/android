using System;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms.Bluetooth;

public sealed class ScanOptions<TDevice>
{
    public TimeSpan ScanTime { get; set; }
    public Action<TDevice>? OnDeviceDiscovered { get; set; }
}
