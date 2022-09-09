namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms
{
    public class CdpBluetoothDevice
    {
        public string? Name { get; init; }
        public string? Alias { get; init; }
        public string? Address { get; init; }

        public byte[]? BeaconData { get; init; }
    }
}
