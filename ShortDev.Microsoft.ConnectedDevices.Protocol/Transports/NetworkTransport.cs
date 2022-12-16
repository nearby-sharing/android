using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Transports;

public sealed class NetworkTransport : ICdpTransport
{
    public static string GetLocalIP()
    {
        var data = Dns.GetHostEntry(string.Empty).AddressList;
        var ips = Dns.GetHostEntry(string.Empty).AddressList
            .Where((x) => x.AddressFamily == AddressFamily.InterNetwork)
            .ToArray();
        if (ips.Length != 1)
            throw new InvalidDataException("Could not resolve ip");

        return ips[0].ToString();
    }

    TcpListener _listener = new(IPAddress.Any, Constants.TcpPort);

    public event DeviceConnectedEventHandler? DeviceConnected;
    public async void Listen(CancellationToken cancellationToken)
    {
        _listener.Start();

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
                RemoteDevice = new()
                {
                    Name = string.Empty,
                    Alias = string.Empty,
                    Address = ((IPEndPoint?)client.Client.RemoteEndPoint)?.Address.ToString() ?? throw new InvalidDataException("No ip address")
                }
            });
        }
    }

    public CdpSocket Connect(CdpDevice device)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _listener.Stop();
    }
}
