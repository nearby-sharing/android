namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

public enum CdpTransportType : ushort
{
    Unknown = 0,
    Udp = 1,
    Tcp = 2,
    Cloud = 3,
    Ble = 4,
    Rfcomm = 5,
    WifiDirect = 6
}
