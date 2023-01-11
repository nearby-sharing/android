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
