using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public sealed class UdpTransport : ICdpTransport, ICdpDiscoverableTransport
{
    readonly UdpClient _client = new(Constants.UdpPort)
    {
        EnableBroadcast = true
    };

    bool _isAdvertising = false;
    CdpAdvertisement? advertisement;
    public void Advertise(CdpAdvertisement options, CancellationToken cancellationToken)
    {
        advertisement = options;
        _isAdvertising = true;
        EnsureListeningInternal(cancellationToken);
        cancellationToken.Register(() => _isAdvertising = false);
    }

    public event DeviceDiscoveredEventHandler? DeviceDiscovered;

    bool _isDiscovering = false;
    public void Discover(CancellationToken cancellationToken)
    {
        _isDiscovering = true;
        EnsureListeningInternal(cancellationToken);
        cancellationToken.Register(() => _isDiscovering = false);

        _ = Task.Run(async () =>
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            new CommonHeader()
            {
                Type = MessageType.Discovery
            }.Write(writer);
            new DiscoveryHeader()
            {
                Type = DiscoveryType.RresenceRequest
            }.Write(writer);

            var msg = stream.ToArray();
            while (!cancellationToken.IsCancellationRequested)
            {
                _client.Send(msg, new IPEndPoint(IPAddress.Broadcast, Constants.UdpPort));
                await Task.Delay(500);
            }
        }, cancellationToken);
    }

    bool _isListening = false;
    async void EnsureListeningInternal(CancellationToken cancellationToken)
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
                    var result = await _client.ReceiveAsync();

                    using MemoryStream stream = new(result.Buffer);
                    using BinaryReader reader = new(stream);

                    if (
                        CommonHeader.TryParse(reader, out var headers, out _) &&
                        headers != null &&
                        headers.Type == MessageType.Discovery
                    )
                    {
                        DiscoveryHeader discoveryHeaders = DiscoveryHeader.Parse(reader);
                        if (_isAdvertising && discoveryHeaders.Type == DiscoveryType.RresenceRequest)
                            SendRresenceResponse(result.RemoteEndPoint.Address);
                        else if (_isDiscovering && discoveryHeaders.Type == DiscoveryType.RresenceResponse)
                        {
                            var response = PresenceResponse.Parse(reader);
                            DeviceDiscovered?.Invoke(
                                this, new CdpDevice()
                                {
                                    Name = response.DeviceName,
                                    Address = result.RemoteEndPoint.Address.ToString()
                                },
                                new CdpAdvertisement(
                                    response.DeviceType,
                                    null,
                                    response.DeviceName
                                )
                            );
                        }
                    }
                }
            }, cancellationToken);
        }
        finally
        {
            _isListening = false;
        }
    }

    void SendRresenceResponse(IPAddress device)
    {
        if (advertisement == null)
            return;

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        new CommonHeader()
        {
            Type = MessageType.Discovery
        }.Write(writer);
        new DiscoveryHeader()
        {
            Type = DiscoveryType.RresenceResponse
        }.Write(writer);
        new PresenceResponse()
        {
            ConnectionMode = Messages.Connection.ConnectionMode.Proximal,
            DeviceName = advertisement.DeviceName,
            DeviceType = advertisement.DeviceType
        }.Write(writer);

        _client.Send(stream.ToArray(), new IPEndPoint(device, Constants.UdpPort));
    }

    #region NotImplemented
    public CdpSocket Connect(CdpDevice device)
        => throw new NotImplementedException();

    public event DeviceConnectedEventHandler? DeviceConnected;
    public void Listen(CancellationToken cancellationToken) { }
    #endregion

    public void Dispose() { }
}
