using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public interface ICdpTransport : IDisposable
{
    CdpTransportType TransportType { get; }

    EndpointInfo GetEndpoint();

    Task<CdpSocket> ConnectAsync(EndpointInfo endpoint);

    async Task<CdpSocket> ConnectAsync(EndpointInfo endpoint, EndpointMetadata? metadata)
        => await ConnectAsync(endpoint);

    async Task<CdpSocket?> TryConnectAsync(EndpointInfo endpoint, EndpointMetadata? metadata, TimeSpan timeout)
    {
        try
        {
            return await ConnectAsync(endpoint, metadata).AwaitWithTimeout(timeout).ConfigureAwait(false);
        }
        catch { }
        return null;
    }

    public event DeviceConnectedEventHandler? DeviceConnected;
    Task Listen(CancellationToken cancellationToken);
}
