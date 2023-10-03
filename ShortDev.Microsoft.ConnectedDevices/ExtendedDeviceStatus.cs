using System;

namespace ShortDev.Microsoft.ConnectedDevices;

[Flags]
public enum ExtendedDeviceStatus
{
    None = 0,
    /// <summary>
    /// Hosted by remote session.
    /// </summary>
    RemoteSessionsHosted = 1,
    /// <summary>
    /// Indicates the device does not have session hosting status available. <br/>
    /// Windows devices prior to Windows 10 v1803 operating system and Windows Server v1803 operating system do not provide session hosting status.
    /// </summary>
    RemoteSessionsNotHosted = 2,
    /// <summary>
    /// Indicates the device supports NearShare if the user is the same for the other device.
    /// </summary>
    NearShareAuthPolicySameUser = 4,
    /// <summary>
    /// Indicates the device supports NearShare. <br/>
    /// Windows devices prior to Windows 10 v1803 and Windows Server v1803 do not support NearShare.
    /// </summary>
    NearShareAuthPolicyPermissive = 8,



    /// <summary>
    /// !! Undocumented !!
    /// </summary>
    NearShareAuthPolicyFamily = 0x10,
    /// <summary>
    /// !! Undocumented !!
    /// </summary>
    WiFiDirectHostingAllowed = NearShareAuthPolicyFamily,
    /// <summary>
    /// !! Undocumented !!
    /// </summary>
    UdpDiscoveryFixup = 0x20
}
