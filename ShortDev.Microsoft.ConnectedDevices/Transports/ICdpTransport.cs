using ShortDev.Microsoft.ConnectedDevices.Platforms;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public interface ICdpTransport : IDisposable
{
    Task<CdpSocket> ConnectAsync(CdpDevice device);

    public event DeviceConnectedEventHandler? DeviceConnected;
    void Listen(CancellationToken cancellationToken);
}
