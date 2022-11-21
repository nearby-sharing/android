namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;

public sealed class CdpBluetoothDevice : CdpDevice
{
    public byte[]? BeaconData { get; init; }
}
