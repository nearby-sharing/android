using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;
using System.Net;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Network;

partial class NetworkTransport
{
    PresenceResponse? _presenceResponse;
    public ValueTask StartAdvertisement(LocalDeviceInfo deviceInfo, CancellationToken cancellationToken)
    {
        _presenceResponse = PresenceResponse.Create(deviceInfo);
        DiscoveryMessageReceived += OnMessage;
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAdvertisement(CancellationToken cancellationToken)
    {
        DiscoveryMessageReceived -= OnMessage;
        return ValueTask.CompletedTask;
    }

    void OnMessage(IPAddress address, DiscoveryHeader header, ref EndianReader reader)
    {
        if (header.Type != DiscoveryType.PresenceRequest)
            return;

        if (_presenceResponse is null)
            return;

        SendPresenceResponse(address, _presenceResponse);
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
}
