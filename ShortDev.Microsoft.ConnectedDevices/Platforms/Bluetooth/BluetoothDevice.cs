namespace ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;

public sealed class BluetoothDevice : CdpDevice
{
    public byte[]? BeaconData { get; init; }
}
