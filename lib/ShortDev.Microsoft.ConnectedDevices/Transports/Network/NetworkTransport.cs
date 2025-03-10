﻿using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Network;

public sealed partial class NetworkTransport(
    INetworkHandler handler,
    int tcpPort = Constants.TcpPort, int udpPort = Constants.UdpPort
) : ICdpTransport, ICdpDiscoverableTransport
{
    public int TcpPort { get; } = tcpPort;
    public int UdpPort { get; } = udpPort;

    public INetworkHandler Handler { get; } = handler;

    public CdpTransportType TransportType { get; } = CdpTransportType.Tcp;
    public EndpointInfo GetEndpoint()
        => new(TransportType, Handler.GetLocalIp().ToString(), TcpPort.ToString(CultureInfo.InvariantCulture));

    #region Listen
    public event DeviceConnectedEventHandler? DeviceConnected;

    BackgroundAction? _listenTask;
    public ValueTask StartListen(CancellationToken cancellationToken)
        => BackgroundAction.Start(ref _listenTask, Listen, cancellationToken);

    public ValueTask StopListen(CancellationToken cancellationToken)
        => BackgroundAction.Stop(ref _listenTask, cancellationToken);

    TcpListener? _listener;
    async Task Listen(CancellationToken cancellationToken)
    {
        var listener = _listener ??= new(IPAddress.Any, TcpPort);
        listener.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);

                if (client.Client.RemoteEndPoint is not IPEndPoint endPoint)
                    return;

                client.NoDelay = true;
                var stream = client.GetStream();
                DeviceConnected?.Invoke(this, new()
                {
                    Close = client.Close,
                    InputStream = stream,
                    OutputStream = stream,
                    Endpoint = new EndpointInfo(
                        TransportType,
                        endPoint.Address.ToString(),
                        TcpPort.ToString(CultureInfo.InvariantCulture)
                    )
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }
    #endregion

    public async Task<CdpSocket> ConnectAsync(EndpointInfo endpoint, CancellationToken cancellationToken = default)
    {
        // ToDo: If the windows machine tries to connect back it uses the port assigned here not 5040!!
        TcpClient client = new();
        await client.ConnectAsync(endpoint.ToIPEndPoint(), cancellationToken).ConfigureAwait(false);
        client.NoDelay = true;
        return new()
        {
            Endpoint = endpoint,
            InputStream = client.GetStream(),
            OutputStream = client.GetStream(),
            Close = client.Close,
        };
    }

    public void Dispose()
    {
        DeviceConnected = null;
        DeviceDiscovered = null;
        DiscoveryMessageReceived = null;

        if (_listener != null)
        {
            _listener.Stop();
            _listener.Dispose();
            _listener = null;
        }

        _udpclient.Dispose();
    }
}
