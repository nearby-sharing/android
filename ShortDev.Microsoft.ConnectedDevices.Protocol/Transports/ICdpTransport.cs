using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using System;
using System.Threading;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Transports;

public interface ICdpTransport : IDisposable
{
    CdpSocket Connect(CdpDevice device);

    public event DeviceConnectedEventHandler? DeviceConnected;
    void Listen(CancellationToken cancellationToken);
}
