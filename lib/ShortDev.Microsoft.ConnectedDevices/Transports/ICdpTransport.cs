namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public interface ICdpTransport : IDisposable
{
    CdpTransportType TransportType { get; }
    bool IsEnabled => true;

    EndpointInfo GetEndpoint();

    Task<CdpSocket> ConnectAsync(EndpointInfo endpoint, CancellationToken cancellationToken = default);
    async Task<CdpSocket?> TryConnectAsync(EndpointInfo endpoint, TimeSpan timeout)
    {
        try
        {
            return await ConnectAsync(endpoint).AwaitWithTimeout(timeout).ConfigureAwait(false);
        }
        catch { }
        return null;
    }

    public event DeviceConnectedEventHandler? DeviceConnected;
    Task Listen(CancellationToken cancellationToken);
}
