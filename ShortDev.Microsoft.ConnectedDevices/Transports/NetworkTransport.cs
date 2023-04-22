﻿using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Platforms.Network;
using ShortDev.Networking;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public sealed class NetworkTransport : ICdpTransport, ICdpDiscoverableTransport
{
    public CdpTransportType TransportType { get; } = CdpTransportType.Tcp;

    public INetworkHandler Handler { get; }
    public NetworkTransport(INetworkHandler handler)
    {
        Handler = handler;
    }

    readonly TcpListener _listener = new(IPAddress.Any, Constants.TcpPort);

    public event DeviceConnectedEventHandler? DeviceConnected;

    public async void Listen(CancellationToken cancellationToken)
    {
        _listener.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                var stream = client.GetStream();
                DeviceConnected?.Invoke(this, new()
                {
                    TransportType = CdpTransportType.Tcp,
                    Close = client.Close,
                    InputStream = stream,
                    OutputStream = stream,
                    RemoteDevice = new(
                        null, // ToDo: ToDo!!
                        DeviceType.Invalid,
                        new EndpointInfo(
                            TransportType,
                            ((IPEndPoint?)client.Client.RemoteEndPoint)?.Address.ToString() ?? throw new InvalidDataException("No ip address"),
                            Constants.TcpPort.ToString()
                        )
                    )
                });
            }
        }
        catch (OperationCanceledException) { }
    }

    public async Task<CdpSocket> ConnectAsync(CdpDevice device)
    {
        // ToDo: If the windows machine tries to connect back it uses the port assigned here not 5040!!
        TcpClient client = new();
        await client.ConnectAsync(device.Endpoint.ToIPEndPoint());
        return new()
        {
            TransportType = TransportType,
            RemoteDevice = device,
            InputStream = client.GetStream(),
            OutputStream = client.GetStream(),
            Close = client.Close,
        };
    }

    public void Dispose()
    {
        DeviceConnected = null;
        _listener.Stop();
    }

    public EndpointInfo GetEndpoint()
        => new(TransportType, Handler.GetLocalIp(), Constants.TcpPort.ToString());

    #region Discovery (Udp)

    readonly UdpClient _udpclient = new(Constants.UdpPort)
    {
        EnableBroadcast = true
    };

    bool _isAdvertising = false;
    CdpAdvertisement? advertisement;
    public void Advertise(CdpAdvertisement options, CancellationToken cancellationToken)
    {
        advertisement = options;
        _isAdvertising = true;
        EnsureListeningUdp(cancellationToken);
        cancellationToken.Register(() => _isAdvertising = false);
    }

    public event DeviceDiscoveredEventHandler? DeviceDiscovered;

    bool _isDiscovering = false;
    public void Discover(CancellationToken cancellationToken)
    {
        _isDiscovering = true;
        EnsureListeningUdp(cancellationToken);
        cancellationToken.Register(() => _isDiscovering = false);

        _ = Task.Run(async () =>
        {
            var msg = GeneratePresenceRequest();
            while (!cancellationToken.IsCancellationRequested)
            {
                _udpclient.Send(msg.AsSpan(), new IPEndPoint(IPAddress.Broadcast, Constants.UdpPort));
                await Task.Delay(500);
            }
        }, cancellationToken);

        static EndianBuffer GeneratePresenceRequest()
        {
            EndianWriter writer = new(Endianness.BigEndian);
            new CommonHeader()
            {
                Type = MessageType.Discovery
            }.Write(writer);
            new DiscoveryHeader()
            {
                Type = DiscoveryType.PresenceRequest
            }.Write(writer);
            return writer.Buffer;
        }
    }

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
        finally
        {
            _isListening = false;
        }

        void HandleMsg(UdpReceiveResult result)
        {
            EndianReader reader = new(Endianness.BigEndian, result.Buffer);
            if (
                CommonHeader.TryParse(reader, out var headers, out _) &&
                headers != null &&
                headers.Type == MessageType.Discovery
            )
            {
                DiscoveryHeader discoveryHeaders = DiscoveryHeader.Parse(reader);
                if (_isAdvertising && discoveryHeaders.Type == DiscoveryType.PresenceRequest)
                    SendPresenceResponse(result.RemoteEndPoint.Address);
                else if (_isDiscovering && discoveryHeaders.Type == DiscoveryType.PresenceResponse)
                {
                    var response = PresenceResponse.Parse(reader);
                    DeviceDiscovered?.Invoke(this,
                        new CdpDevice(
                            response.DeviceName,
                            DeviceType.Invalid, // ToDo: ToDo
                            EndpointInfo.FromTcp(result.RemoteEndPoint)
                        ),
                        new CdpAdvertisement(
                            response.DeviceType,
                            null,
                            response.DeviceName
                        )
                    );
                }
            }
        }

        void SendPresenceResponse(IPAddress device)
        {
            if (advertisement == null)
                return;

            EndianWriter writer = new(Endianness.BigEndian);
            new CommonHeader()
            {
                Type = MessageType.Discovery
            }.Write(writer);
            new DiscoveryHeader()
            {
                Type = DiscoveryType.PresenceResponse
            }.Write(writer);
            new PresenceResponse()
            {
                ConnectionMode = Messages.Connection.ConnectionMode.Proximal,
                DeviceName = advertisement.DeviceName,
                DeviceType = advertisement.DeviceType
            }.Write(writer);

            _udpclient.Send(writer.Buffer.AsSpan(), new IPEndPoint(device, Constants.UdpPort));
        }
    }

    #endregion
}
