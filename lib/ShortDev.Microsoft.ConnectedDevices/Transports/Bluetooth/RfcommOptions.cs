namespace ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;

public sealed class RfcommOptions
{
    public required string ServiceId { get; init; }
    public required string ServiceName { get; init; }
    public required Action<CdpSocket> SocketConnected { get; init; }
}
