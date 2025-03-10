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
            await Task.WhenAll(
                EnsureListeningUdp(cancellationToken),
                RunPresenceSendLoop()
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        finally
        {
            DiscoveryMessageReceived -= OnMessage;
        }

        async Task RunPresenceSendLoop()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                SendPresenceRequest();
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
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

    delegate void DiscoveryMessageReceivedHandler(IPAddress address, DiscoveryHeader header, ref EndianReader reader);
    event DiscoveryMessageReceivedHandler? DiscoveryMessageReceived;
}
