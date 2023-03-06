using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;

public interface IBluetoothHandler : ICdpPlatformHandler
{
    Task ScanBLeAsync(ScanOptions scanOptions, CancellationToken cancellationToken = default);
    Task AdvertiseBLeBeaconAsync(AdvertiseOptions options, CancellationToken cancellationToken = default);

    bool SupportsRfcomm { get; }
    Task<CdpSocket> ConnectRfcommAsync(CdpDevice device, RfcommOptions options, CancellationToken cancellationToken = default);
    Task ListenRfcommAsync(RfcommOptions options, CancellationToken cancellationToken = default);

    bool SupportsGatt { get; }
    Task<CdpSocket> ConnectGattAsync(CdpDevice device, GattOptions options, CancellationToken cancellationToken = default);
}
