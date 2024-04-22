using System;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;

public sealed class RfcommOptions
{
    public string? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public Action<CdpSocket>? SocketConnected { get; set; }
}
