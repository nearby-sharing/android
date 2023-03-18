using System;

namespace ShortDev.Microsoft.ConnectedDevices;

[Flags]
public enum ExtendedDeviceStatus
{
    RemoteSessionsHosted = 1,
    RemoteSessionsNotHosted = 2,
    NearShareAuthPolicySameUser = 4,
    NearShareAuthPolicyPermissive = 8,
    NearShareAuthPolicyFamily = 0x10,

    WiFiDirectHostingAllowed = NearShareAuthPolicyFamily,
    UdpDiscoveryFixup = 0x20
}
