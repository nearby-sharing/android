using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;
using System.Net;
using System.Net.Sockets;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Network;

public sealed class NetworkTransport(INetworkHandler handler) : ICdpTransport, ICdpDiscoverableTransport
{
    readonly TcpListener _listener = new(IPAddress.Any, Constants.TcpPort);
    public INetworkHandler Handler { get; } = handler;

    public CdpTransportType TransportType { get; } = CdpTransportType.Tcp;
    public EndpointInfo GetEndpoint()
        => new(TransportType, Handler.GetLocalIp().ToString(), Constants.TcpPort.ToString());

    public event DeviceConnectedEventHandler? DeviceConnected;
    public async void Listen(CancellationToken cancellationToken)
    {
        _listener.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);

                if (client.Client.RemoteEndPoint is not IPEndPoint endPoint)
                    return;

                var stream = client.GetStream();
                DeviceConnected?.Invoke(this, new()
                {
                    Close = client.Close,
                    InputStream = stream,
                    OutputStream = stream,
                    Endpoint = new EndpointInfo(
                        TransportType,
                        endPoint.Address.ToString(),
                        Constants.TcpPort.ToString()
                    )
                });
            }
        }
        catch (OperationCanceledException) { }
    }

    public async Task<CdpSocket> ConnectAsync(EndpointInfo endpoint)
    {
        // ToDo: If the windows machine tries to connect back it uses the port assigned here not 5040!!
        TcpClient client = new();
        await client.ConnectAsync(endpoint.ToIPEndPoint());
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

    public void Advertise(LocalDeviceInfo deviceInfo, CancellationToken cancellationToken)
    {
        var presenceResponse = PresenceResponse.Create(deviceInfo);

        DiscoveryMessageReceived += OnMessage;
        cancellationToken.Register(() => DiscoveryMessageReceived -= OnMessage);
        EnsureListeningUdp(cancellationToken);

        void OnMessage(IPEndPoint remoteEndPoint, DiscoveryHeader header, EndianReader reader)
        {
            if (header.Type != DiscoveryType.PresenceRequest)
                return;

            SendPresenceResponse(remoteEndPoint.Address, presenceResponse);
        }
    }

    public event DeviceDiscoveredEventHandler? DeviceDiscovered;
    public async void Discover(CancellationToken cancellationToken)
    {
        DiscoveryMessageReceived += OnMessage;
        EnsureListeningUdp(cancellationToken);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                SendPresenceRequest();
                await Task.Delay(500, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        finally
        {
            DiscoveryMessageReceived -= OnMessage;
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

    bool _isListening = false;
    async void EnsureListeningUdp(CancellationToken cancellationToken)
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
                    var result = await _udpclient.ReceiveAsync();
                    HandleMsg(result);
                }
            }, cancellationToken);
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

        _listener.Stop();
        _udpclient.Dispose();
    }
}
