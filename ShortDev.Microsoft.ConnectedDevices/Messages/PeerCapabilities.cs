using System;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

[Flags]
public enum PeerCapabilities : ulong
{
    UpgradeSupport = 1,
    HandleDeviceInfo = 4,
    ExtendedAdditionHeader = 0x10,
    All = 0x1f
}

[Flags]
public enum Header129Values : ulong
{
    Default = 0x30000001,
    All = 0x70000003
}

[Flags]
public enum ConnectFlags
{
    Cloud = 1,
    Rfcomm = 2,
    WiFiDirect = 4,
    Udp = 8,
    Tcp = 16,
    BLeGatt = 32
}
