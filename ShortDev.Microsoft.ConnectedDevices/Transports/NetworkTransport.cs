using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Platforms.Network;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public sealed class NetworkTransport : ICdpTransport
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
                        null,
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
}
