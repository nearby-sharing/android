namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public interface ICdpTransport : IDisposable
{
    CdpTransportType TransportType { get; }

    EndpointInfo GetEndpoint();

    Task<CdpSocket> ConnectAsync(EndpointInfo endpoint);
    async Task<CdpSocket?> TryConnectAsync(EndpointInfo endpoint, TimeSpan timeout)
    {
        try
        {
            return await ConnectAsync(endpoint).WithTimeout(timeout).ConfigureAwait(false);
        }
        catch { }
        return null;
    }

    public event DeviceConnectedEventHandler? DeviceConnected;
    Task Listen(CancellationToken cancellationToken);
}
