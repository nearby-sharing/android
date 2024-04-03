using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Session.Channels;
internal sealed class ClientChannelHandler(CdpSession session) : ChannelHandler(session)
{
    protected override void HandleMessageInternal(CdpSocket socket, CommonHeader header, ControlHeader controlHeader, ref EndianReader reader)
    {
        if (controlHeader.MessageType != ControlMessageType.StartChannelResponse)
            return;

        var msg = StartChannelResponse.Parse(ref reader);
        OnStartChannelResponseInternal?.Invoke(header, msg);
    }

    event Action<CommonHeader, StartChannelResponse>? OnStartChannelResponseInternal;
    public Task<StartChannelResponse> WaitForChannelResponse(ulong requestId, CancellationToken cancellationToken)
    {
        TaskCompletionSource<StartChannelResponse> promise = new();
        void callback(CommonHeader header, StartChannelResponse response)
        {
            if (header.TryGetReplyToId() == requestId)
                promise.SetResult(response);

            OnStartChannelResponseInternal -= callback;
        }
        OnStartChannelResponseInternal += callback;

        cancellationToken.Register(() =>
        {
            OnStartChannelResponseInternal -= callback;
            promise.TrySetCanceled();
        });

        return promise.Task;
    }

    public async Task<CdpChannel> CreateChannelAsync(string appId, string appName, CdpAppBase handler, CdpSocket socket, CancellationToken cancellationToken = default)
    {
        var requestId = SendChannelRequest(socket, appId, appName);

        var response = await WaitForChannelResponse(requestId, cancellationToken);
        response.ThrowOnError();

        var channel = CdpChannel.CreateClientChannel(this, socket, response, handler);
        _channelRegistry.Add(channel.ChannelId, channel);
        return channel;
    }

    ulong SendChannelRequest(CdpSocket socket, string appId, string appName)
    {
        EndianWriter writer = new(Endianness.BigEndian);
        new ControlHeader()
        {
            MessageType = ControlMessageType.StartChannelRequest
        }.Write(writer);
        new StartChannelRequest()
        {
            Id = appId,
            Name = appName
        }.Write(writer);

        CommonHeader header = new()
        {
            Type = MessageType.Control
        };
        Session.SendMessage(socket, header, writer, supplyRequestId: true);

        return header.RequestID;
    }
}
