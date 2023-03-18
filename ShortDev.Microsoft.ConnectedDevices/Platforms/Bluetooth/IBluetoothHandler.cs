using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;

public interface IBluetoothHandler : ICdpPlatformHandler
{
    Task ScanBLeAsync(ScanOptions scanOptions, CancellationToken cancellationToken = default);
    Task<CdpSocket> ConnectRfcommAsync(CdpDevice device, RfcommOptions options, CancellationToken cancellationToken = default);

    Task AdvertiseBLeBeaconAsync(AdvertiseOptions options, CancellationToken cancellationToken = default);
    Task ListenRfcommAsync(RfcommOptions options, CancellationToken cancellationToken = default);

    PhysicalAddress MacAddress { get; }
}
