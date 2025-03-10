using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;
using System.Net;
using System.Net.Sockets;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Network;

partial class NetworkTransport
{
    readonly UdpClient _udpclient = CreateUdpClient(udpPort);

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
            DiscoveryMessageReceived?.Invoke(result.RemoteEndPoint.Address, discoveryHeaders, ref reader);
        }
    }

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

        new UdpFragmentSender(_udpclient, new IPEndPoint(IPAddress.Broadcast, UdpPort))
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

        new UdpFragmentSender(_udpclient, new IPEndPoint(device, UdpPort))
            .SendMessage(header, payloadWriter.Buffer.AsSpan());
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
