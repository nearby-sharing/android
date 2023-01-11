using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;

public interface IBluetoothHandler : ICdpPlatformHandler
{
    Task ScanBLeAsync(ScanOptions<BluetoothDevice> scanOptions, CancellationToken cancellationToken = default);
    Task<CdpSocket> ConnectRfcommAsync(BluetoothDevice device, RfcommOptions options, CancellationToken cancellationToken = default);

    Task AdvertiseBLeBeaconAsync(AdvertiseOptions options, CancellationToken cancellationToken = default);
    Task ListenRfcommAsync(RfcommOptions options, CancellationToken cancellationToken = default);
}
