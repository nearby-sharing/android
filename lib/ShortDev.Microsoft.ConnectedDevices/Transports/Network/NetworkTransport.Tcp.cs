using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Network;

partial class NetworkTransport
{
    TcpListener? _listener;
    async Task ListenTcp(CancellationToken cancellationToken)
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
}
