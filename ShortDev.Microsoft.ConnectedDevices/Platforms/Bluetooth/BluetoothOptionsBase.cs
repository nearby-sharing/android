using System;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;

public abstract class BluetoothOptionsBase
{
    public Action<CdpSocket>? SocketConnected { get; set; }
}
