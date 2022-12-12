using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;

public sealed class NetworkAdvertisement : IAdvertiser
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

    TcpListener _listener = new(IPAddress.Any, 5040);

    CancellationTokenSource? cancellationTokenSource;
    public async void StartAdvertisement(CdpDeviceAdvertiseOptions options)
    {
        cancellationTokenSource = new();

        _listener.Start();

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync(cancellationTokenSource.Token);
            var stream = client.GetStream();
            OnDeviceConnected?.Invoke(new()
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

    public event Action<CdpSocket>? OnDeviceConnected;

    public void StopAdvertisement()
    {
        // Called from "StartAdvertisement"!
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Dispose();
        }
        _listener.Stop();
    }
}
