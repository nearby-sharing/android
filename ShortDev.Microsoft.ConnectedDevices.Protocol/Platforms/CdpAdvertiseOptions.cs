namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms
{
    public sealed class CdpAdvertiseOptions
    {
        public int ManufacturerId { get; set; }
        public byte[]? BeaconData { get; set; }
    }
}
