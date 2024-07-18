namespace ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
public sealed class WiFiDirectTransport(IWiFiDirectHandler handler) : ICdpTransport
{
    readonly IWiFiDirectHandler _handler = handler;

    public CdpTransportType TransportType { get; } = CdpTransportType.WifiDirect;

    public event DeviceConnectedEventHandler? DeviceConnected;

    public Task<CdpSocket> ConnectAsync(EndpointInfo endpoint)
    {
        throw new NotImplementedException();
    }

    public Task Listen(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public EndpointInfo GetEndpoint()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
        => _handler.Dispose();
}
