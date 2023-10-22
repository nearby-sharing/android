using ShortDev.Microsoft.ConnectedDevices.Transports;
using System;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;

public sealed class ScanOptions
{
    public Action<BLeBeacon>? OnDeviceDiscovered { get; set; }
}
