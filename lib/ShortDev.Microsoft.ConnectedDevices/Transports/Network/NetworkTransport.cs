using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Network;

public sealed class NetworkTransport(INetworkHandler handler) : ICdpTransport, ICdpDiscoverableTransport
{
    readonly TcpListener _listener = new(IPAddress.Any, Constants.TcpPort);
    public INetworkHandler Handler { get; } = handler;

    public CdpTransportType TransportType { get; } = CdpTransportType.Tcp;
    public EndpointInfo GetEndpoint()
        => new(TransportType, Handler.GetLocalIp().ToString(), Constants.TcpPort.ToString(CultureInfo.InvariantCulture));

    public event DeviceConnectedEventHandler? DeviceConnected;
    public async Task Listen(CancellationToken cancellationToken)
    {
        _listener.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);

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
                        Constants.TcpPort.ToString(CultureInfo.InvariantCulture)
                    )
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

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

    #region Discovery (Udp)

    readonly UdpClient _udpclient = new(Constants.UdpPort)
    {
        EnableBroadcast = true
    };

    public async Task Advertise(LocalDeviceInfo deviceInfo, CancellationToken cancellationToken)
    {
        var presenceResponse = PresenceResponse.Create(deviceInfo);

        DiscoveryMessageReceived += OnMessage;
        try
        {
            await EnsureListeningUdp(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            DiscoveryMessageReceived -= OnMessage;
        }

        void OnMessage(IPEndPoint remoteEndPoint, DiscoveryHeader header, EndianReader reader)
        {
            if (header.Type != DiscoveryType.PresenceRequest)
                return;

            SendPresenceResponse(remoteEndPoint.Address, presenceResponse);
        }
    }

    public event DeviceDiscoveredEventHandler? DeviceDiscovered;
    public async Task Discover(CancellationToken cancellationToken)
    {
        DiscoveryMessageReceived += OnMessage;
        try
        {
            await Task.WhenAll(
                EnsureListeningUdp(cancellationToken),
                RunPresenceSendLoop()
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        finally
        {
            DiscoveryMessageReceived -= OnMessage;
        }

        async Task RunPresenceSendLoop()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                SendPresenceRequest();
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }

        void OnMessage(IPEndPoint remoteEndPoint, DiscoveryHeader header, EndianReader reader)
        {
            if (header.Type != DiscoveryType.PresenceResponse)
                return;

            var response = PresenceResponse.Parse(ref reader);
            DeviceDiscovered?.Invoke(
                this,
                new CdpDevice(
                    response.DeviceName,
                    response.DeviceType,
                    EndpointInfo.FromTcp(remoteEndPoint)
                )
            );
        }
    }

    delegate void DiscoveryMessageReceivedHandler(IPEndPoint remoteEndPoint, DiscoveryHeader header, EndianReader reader);
    event DiscoveryMessageReceivedHandler? DiscoveryMessageReceived;

    bool _isListening;
    async Task EnsureListeningUdp(CancellationToken cancellationToken)
    {
        if (_isListening)
            return;

        _isListening = true;
        try
        {
            await Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await _udpclient.ReceiveAsync().ConfigureAwait(false);
                    HandleMsg(result);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException) { }
        catch (SocketException ex)
        {
            if (ex.SocketErrorCode is not (SocketError.Shutdown or SocketError.OperationAborted))
                throw;
        }
        finally
        {
            _isListening = false;
        }

        void HandleMsg(UdpReceiveResult result)
        {
            EndianReader reader = new(Endianness.BigEndian, result.Buffer);
            if (!CommonHeader.TryParse(ref reader, out var headers, out _) || headers.Type != MessageType.Discovery)
                return;

            DiscoveryHeader discoveryHeaders = DiscoveryHeader.Parse(ref reader);
            DiscoveryMessageReceived?.Invoke(result.RemoteEndPoint, discoveryHeaders, reader);
        }
    }
    #endregion

    void SendPresenceRequest()
    {
        CommonHeader header = new()
        {
            Type = MessageType.Discovery,
        };

        EndianWriter payloadWriter = new(Endianness.BigEndian);
        new DiscoveryHeader()
        {
            Type = DiscoveryType.PresenceRequest
        }.Write(payloadWriter);

        new UdpFragmentSender(_udpclient, new IPEndPoint(IPAddress.Broadcast, Constants.UdpPort))
            .SendMessage(header, payloadWriter.Buffer.AsSpan());
    }

    void SendPresenceResponse(IPAddress device, PresenceResponse response)
    {
        CommonHeader header = new()
        {
            Type = MessageType.Discovery
        };

        EndianWriter payloadWriter = new(Endianness.BigEndian);
        new DiscoveryHeader()
        {
            Type = DiscoveryType.PresenceResponse
        }.Write(payloadWriter);
        response.Write(payloadWriter);

        new UdpFragmentSender(_udpclient, new IPEndPoint(device, Constants.UdpPort))
            .SendMessage(header, payloadWriter.Buffer.AsSpan());
    }

    sealed class UdpFragmentSender(UdpClient client, IPEndPoint receiver) : IFragmentSender
    {
        public void SendFragment(ReadOnlySpan<byte> fragment)
            => client.Send(fragment, receiver);
    }

    public void Dispose()
    {
        DeviceConnected = null;
        DeviceDiscovered = null;
        DiscoveryMessageReceived = null;

        _listener.Dispose();
        _udpclient.Dispose();
    }
}
