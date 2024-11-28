namespace ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;

enum MessageValueType : byte
{
    InvalidMessageValueType,
    RolePreference,
    RoleDecision,
    DeviceAddress,
    GODeviceAddress,
    GOSSID,
    GOPreSharedKey
}
