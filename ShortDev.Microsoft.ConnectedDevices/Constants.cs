namespace ShortDev.Microsoft.ConnectedDevices;

public static class Constants
{
    public const int DiscoveryPort = 5050;
    public const int TcpPort = 5040;

    public const byte ProtocolVersion = 3;
    public const ushort Signature = 0x3030;

    public const int BLeBeaconManufacturerId = /* Microsoft */ 0x6;
    public const string RfcommServiceId = "c7f94713-891e-496a-a0e7-983a0946126e";
    public const string RfcommServiceName = "CDP Proximal Transport";

    public const int DefaultMessageFragmentSize = 16384;
    public const int HMacSize = 32;
    public const int IVSize = 16;

    public const int KB = 1024;
    public const int MB = 1024 * KB;
    public const int GB = 1024 * MB;
}
