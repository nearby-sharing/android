using System.Net.NetworkInformation;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery
{
    public sealed record CdpDeviceAdvertiseOptions(DeviceType DeviceType, PhysicalAddress MacAddress, string DeviceName);
}
