using System;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;

public sealed class CdpScanOptions<TDevice>
{
    public TimeSpan ScanTime { get; set; }
    public Action<TDevice>? OnDeviceDiscovered { get; set; }
}
