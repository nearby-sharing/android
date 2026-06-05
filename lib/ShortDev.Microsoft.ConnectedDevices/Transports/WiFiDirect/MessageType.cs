namespace ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;

enum MessageType : byte
{
    Invalid,
    ClientAvailableForUpgrade,
    HostGetUpgradeEndpoints,
    ClientFinalizeUpgrade
}
