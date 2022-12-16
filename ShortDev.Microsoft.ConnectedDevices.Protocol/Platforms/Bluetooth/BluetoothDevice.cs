namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms.Bluetooth;

public sealed class BluetoothDevice : CdpDevice
{
    public byte[]? BeaconData { get; init; }
}
