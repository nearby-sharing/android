using System;

namespace ShortDev.Microsoft.ConnectedDevices;

public enum DeviceType : short
{
    Invalid = -1,

    XboxOne = 1,

    Xbox360 = 2,
    [Obsolete("Legacy")]
    WindowsDesktopLegacy = 3,
    [Obsolete("Legacy")]
    WindowsStoreLegacy = 4,
    [Obsolete("Legacy")]
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
        if (deviceType == DeviceType.Linux)
            return DdsPlatformType.Linux;

        if (deviceType == DeviceType.Android)
            return DdsPlatformType.Android;

        if (deviceType == DeviceType.iPad || deviceType == DeviceType.iPhone)
            return DdsPlatformType.iOS;

        return DdsPlatformType.Windows;
    }

    public static bool IsMobile(this DeviceType deviceType)
        => deviceType is DeviceType.Android or DeviceType.iPhone or DeviceType.Windows10Phone;
}


public enum DdsPlatformType
{
    Unknown = 0,
    Windows = 1,
    Android = 2,
    iOS = 3,
    Linux = 5
}
