using System;

namespace ShortDev.Microsoft.ConnectedDevices;

public enum DeviceType : short
{
    XboxOne = 1,

    Xbox360 = 2,
    [Obsolete]
    WindowsDesktopLegacy = 3,
    [Obsolete]
    WindowsStoreLegacy = 4,
    [Obsolete]
    WindowsPhoneLegacy = 5,

    iPhone = 6,
    iPad = 7,
    Android = 8,
    Windows10Desktop = 9,

    HoloLens = 10,

    Windows10Phone = 11,
    Linux = 12,
    WindowsIoT = 13,
    SurfaceHub = 14,
    WindowsLaptop = 15,
    WindowsTablet = 16
}

public static class DeviceTypeExtensions
{
    public static DdsPlatformType GetPlatformType(this DeviceType deviceType)
    {
        if (deviceType == ConnectedDevices.DeviceType.Linux)
            return DdsPlatformType.Linux;

        if (deviceType == ConnectedDevices.DeviceType.Android)
            return DdsPlatformType.Android;

        if (deviceType == ConnectedDevices.DeviceType.iPad || deviceType == ConnectedDevices.DeviceType.iPhone)
            return DdsPlatformType.iOS;

        return DdsPlatformType.Windows;
    }
}


public enum DdsPlatformType
{
    Unknown = 0,
    Windows = 1,
    Android = 2,
    iOS = 3,
    Linux = 5
}
