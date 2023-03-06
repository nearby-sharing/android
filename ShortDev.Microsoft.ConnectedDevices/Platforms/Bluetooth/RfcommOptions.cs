using System;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;

public sealed class RfcommOptions : BluetoothOptionsBase
{
    public string? ServiceId { get; set; }
    public string? ServiceName { get; set; }
}
