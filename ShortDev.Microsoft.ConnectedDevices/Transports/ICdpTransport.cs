using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public interface ICdpTransport : IDisposable
{
    CdpTransportType TransportType { get; }

    EndpointInfo GetEndpoint();

    Task<CdpSocket> ConnectAsync(CdpDevice device);
    async Task<CdpSocket?> TryConnectAsync(CdpDevice device, TimeSpan timeout)
    {
        try
        {
            return await ConnectAsync(device).WithTimeout(timeout);
        }
        catch (Exception ex)
        {
        }
        return null;
    }

    public event DeviceConnectedEventHandler? DeviceConnected;
    void Listen(CancellationToken cancellationToken);
}
