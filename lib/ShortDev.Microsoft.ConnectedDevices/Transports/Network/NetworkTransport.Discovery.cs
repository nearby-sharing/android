using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;
using System.Net;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Network;

partial class NetworkTransport
{
    BackgroundAction? _discoveryAction;
    public ValueTask StartDiscovery(CancellationToken cancellationToken)
        => BackgroundAction.Start(ref _discoveryAction, Discover, cancellationToken);

    public ValueTask StopDiscovery(CancellationToken cancellationToken)
        => BackgroundAction.Stop(ref _discoveryAction, cancellationToken);

    public event DeviceDiscoveredEventHandler? DeviceDiscovered;
    async Task Discover(CancellationToken cancellationToken)
    {
        DiscoveryMessageReceived += OnMessage;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                SendPresenceRequest();
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        finally
        {
            DiscoveryMessageReceived -= OnMessage;
        }

        void OnMessage(IPAddress address, DiscoveryHeader header, ref EndianReader reader)
        {
            if (header.Type != DiscoveryType.PresenceResponse)
                return;

            var response = PresenceResponse.Parse(ref reader);
            DeviceDiscovered?.Invoke(
                this,
                new CdpDevice(
                    response.DeviceName,
                    response.DeviceType,
                    EndpointInfo.FromTcp(address)
                )
            );
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

    delegate void DiscoveryMessageReceivedHandler(IPAddress address, DiscoveryHeader header, ref EndianReader reader);
    event DiscoveryMessageReceivedHandler? DiscoveryMessageReceived;
}
