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

        _channelRegistry.Create(channelId => CdpChannel.CreateServerChannel(this, socket, request, channelId), out var channelId);

        header.Flags = 0;
        Session.SendMessage(
            socket,
            ref header,
            new ControlHeader()
            {
                MessageType = ControlMessageType.StartChannelResponse
            },
            new StartChannelResponse()
            {
                Result = ChannelResult.Success,
                ChannelId = channelId
            }
        );
    }
}
