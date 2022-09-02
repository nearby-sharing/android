using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms
{
    public interface ICdpBluetoothHandler
    {
        Task ScanForDevicesAsync(CdpScanOptions<CdpBluetoothDevice> scanOptions, CancellationToken cancellationToken = default);
    }
}

namespace System.Runtime.CompilerServices
{
    class IsExternalInit { }
}
