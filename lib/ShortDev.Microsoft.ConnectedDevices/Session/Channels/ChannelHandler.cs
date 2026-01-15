using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Internal;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Session.Channels;
internal abstract class ChannelHandler(CdpSession session) : IDisposable
{
    readonly ILogger _logger = session.Platform.CreateLogger<ChannelHandler>();

    public CdpSession Session { get; } = session;

    public void HandleControl(CdpSocket socket, CommonHeader header, ref HeapEndianReader reader)
    {
        var controlHeader = ControlHeader.Parse(ref reader);
        _logger.ReceivedControlMessage(
            controlHeader.MessageType,
            header.SessionId,
            socket.TransportType
        );

        HandleMessageInternal(socket, header, controlHeader, ref reader);
    }

    protected abstract void HandleMessageInternal(CdpSocket socket, CommonHeader header, ControlHeader controlHeader, ref HeapEndianReader reader);

    #region Registry
    protected readonly AutoKeyRegistry<ulong, CdpChannel> _channelRegistry = [];
    public CdpChannel GetChannelById(ulong channelId)
        => _channelRegistry.Get(channelId);

    public void UnregisterChannel(CdpChannel channel)
        => _channelRegistry.Remove(channel.ChannelId);
    #endregion

    public static ChannelHandler Create(CdpSession session)
        => session.SessionId.IsHost switch
        {
            true => new HostChannelHandler(session),
            false => new ClientChannelHandler(session)
        };

    public void Dispose()
    {
        foreach (var channel in _channelRegistry)
            channel.Dispose();
        _channelRegistry.Clear();
    }
}
