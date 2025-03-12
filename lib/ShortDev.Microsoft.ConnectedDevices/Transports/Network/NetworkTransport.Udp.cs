using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;
using System.Net;
using System.Net.Sockets;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Network;

partial class NetworkTransport
{
    readonly UdpClient _udpclient = CreateUdpClient(udpPort);

    async Task ListenUdp(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await _udpclient.ReceiveAsync(cancellationToken).ConfigureAwait(false);
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

        void HandleMsg(UdpReceiveResult result)
        {
            EndianReader reader = new(Endianness.BigEndian, result.Buffer);
            if (!CommonHeader.TryParse(ref reader, out var headers, out _) || headers.Type != MessageType.Discovery)
                return;

            DiscoveryHeader discoveryHeaders = DiscoveryHeader.Parse(ref reader);
            DiscoveryMessageReceived?.Invoke(result.RemoteEndPoint.Address, discoveryHeaders, ref reader);
        }
    }

    static UdpClient CreateUdpClient(int port)
    {
        UdpClient client = new()
        {
            EnableBroadcast = true
        };

        if (OperatingSystem.IsWindows())
        {
            const int SIO_UDP_CONNRESET = -1744830452;
            client.Client.IOControl(SIO_UDP_CONNRESET, [0, 0, 0, 0], null);
        }

        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.Client.Bind(new IPEndPoint(IPAddress.Any, port));

        return client;
    }

    sealed class UdpFragmentSender(UdpClient client, IPEndPoint receiver) : IFragmentSender
    {
        public void SendFragment(ReadOnlySpan<byte> fragment)
            => client.Send(fragment, receiver);
    }
}
