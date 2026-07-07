using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Session.Channels;

internal sealed class HostChannelHandler(CdpSession session) : ChannelHandler(session)
{
    protected override void HandleMessageInternal(CdpSocket socket, CommonHeader header, ControlHeader controlHeader, ref HeapEndianReader reader)
    {
        if (controlHeader.MessageType != ControlMessageType.StartChannelRequest)
            return;

        var request = StartChannelRequest.Parse(ref reader);

        header.AdditionalHeaders.Clear();
        header.SetReplyToId(header.RequestID);
        header.AdditionalHeaders.Add(new(
            (AdditionalHeaderType)129,
            new byte[] { 0x30, 0x0, 0x0, 0x1 }
        ));
        header.RequestID = 0;
        header.Flags = 0;

        (ChannelResult result, ulong channelId) result;
        if (!CdpAppRegistration.TryGetAppFactory(request.Id, request.Name, out var appFactory))
        {
            result = (ChannelResult.Failure_NotFound, 0);
        }
        else
        {
            var channel = _channelRegistry.Create(channelId => CdpChannel.CreateServerChannel(this, socket, channelId), out var channelId);
            appFactory(channel);
            result = (ChannelResult.Success, channelId);
        }

        Session.SendMessage(
            socket,
            ref header,
            new ControlHeader()
            {
                MessageType = ControlMessageType.StartChannelResponse
            },
            new StartChannelResponse()
            {
                Result = result.result,
                ChannelId = result.channelId
            }
        );
    }
}
