using ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;
using System.Net;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Network;

partial class NetworkTransport
{
    BackgroundAction? _advertisementAction;
    public ValueTask StartAdvertisement(LocalDeviceInfo deviceInfo, CancellationToken cancellationToken)
        => BackgroundAction.Start(ref _advertisementAction, token => Advertise(deviceInfo, token), cancellationToken);

    public ValueTask StopAdvertisement(CancellationToken cancellationToken)
        => BackgroundAction.Stop(ref _advertisementAction, cancellationToken);

    async Task Advertise(LocalDeviceInfo deviceInfo, CancellationToken cancellationToken)
    {
        var presenceResponse = PresenceResponse.Create(deviceInfo);

        DiscoveryMessageReceived += OnMessage;
        try
        {
            await EnsureListeningUdp(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            DiscoveryMessageReceived -= OnMessage;
        }

        void OnMessage(IPAddress address, DiscoveryHeader header, ref EndianReader reader)
        {
            if (header.Type != DiscoveryType.PresenceRequest)
                return;

            SendPresenceResponse(address, presenceResponse);
        }
    }
}
