using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public interface ICdpTransport : IDisposable
{
    CdpTransportType TransportType { get; }
    bool IsEnabled => true;

    EndpointInfo GetEndpoint();

    Task<CdpSocket> ConnectAsync(EndpointInfo endpoint, CancellationToken cancellationToken = default);

    async Task<CdpSocket> ConnectAsync(EndpointInfo endpoint, EndpointMetadata? metadata, CancellationToken cancellationToken = default)
        => await ConnectAsync(endpoint, cancellationToken);

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
