using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Session.Channels;
internal sealed class HostChannelHandler(CdpSession session) : ChannelHandler(session)
{
    protected override void HandleMessageInternal(CdpSocket socket, CommonHeader header, ControlHeader controlHeader, ref EndianReader reader)
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

        EndianWriter writer = new(Endianness.BigEndian);
        new ControlHeader()
        {
            MessageType = ControlMessageType.StartChannelResponse
        }.Write(writer);
        new StartChannelResponse()
        {
            Result = ChannelResult.Success,
            ChannelId = channelId
        }.Write(writer);

        header.Flags = 0;
        Session.SendMessage(socket, header, writer);
    }
}
