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
    public INetworkHandler Handler { get; }
    public NetworkTransport(INetworkHandler handler)
    {
        Handler = handler;
    }

    TcpListener _listener = new(IPAddress.Any, Constants.TcpPort);

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
                        string.Empty,
                        ((IPEndPoint?)client.Client.RemoteEndPoint)?.Address.ToString() ?? throw new InvalidDataException("No ip address")
                    )
                });
            }
        }
        catch (OperationCanceledException) { }
    }

    public Task<CdpSocket> ConnectAsync(CdpDevice device)
        => throw new NotImplementedException();

    public void Dispose()
    {
        DeviceConnected = null;
        _listener.Stop();
    }
}
