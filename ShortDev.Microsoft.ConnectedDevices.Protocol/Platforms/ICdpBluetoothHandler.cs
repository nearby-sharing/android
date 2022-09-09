using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms
{
    public interface ICdpBluetoothHandler
    {
        Task ScanBLeAsync(CdpScanOptions<CdpBluetoothDevice> scanOptions, CancellationToken cancellationToken = default);
        Task<CdpRfcommSocket> ConnectRfcommAsync(CdpBluetoothDevice device, CdpRfcommOptions options, CancellationToken cancellationToken = default);

        Task AdvertiseBLeBeaconAsync(CdpAdvertiseOptions options, CancellationToken cancellationToken = default);
        Task ListenRfcommAsync(CdpRfcommOptions options, CancellationToken cancellationToken = default);
    }
}

namespace System.Runtime.CompilerServices
{
    class IsExternalInit { }
}
