using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Session.Channels;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Runtime.CompilerServices;

namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// Provides the interface between a <see cref="CdpAppBase"/> and a <see cref="CdpSession"/>.
/// </summary>
public sealed class CdpChannel : IDisposable
{
    readonly ChannelHandler _handler;
    private CdpChannel(ChannelHandler handler, ulong channelId, CdpSocket socket)
    {
        _handler = handler;

        Session = handler.Session;
        ChannelId = channelId;
        Socket = socket;
    }

    /// <summary>
    /// Get's the corresponding <see cref="CdpSession"/>. <br/>
    /// <br/>
    /// <inheritdoc cref="CdpSession"/>
    /// </summary>
    public CdpSession Session { get; }

    /// <summary>
    /// Get's the corresponding <see cref="CdpSocket"/>. <br/>
    /// <br/>
    /// <inheritdoc cref="CdpSocket" />
    /// </summary>
    public CdpSocket Socket { get; }

    /// <summary>
    /// Get's the unique id for the channel. <br/>
    /// The id is unique as long as the channel is active.
    /// </summary>
    public ulong ChannelId { get; }

    /// <summary>
    /// Raised whenever the channel receives a new message from a remote device.
    /// </summary>
    public event EventHandler<CdpMessage>? MessageReceived;

    internal void HandleMessage(CdpMessage msg)
        => MessageReceived?.Invoke(this, msg);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendMessage<TMessage>(in TMessage message) where TMessage : IBinaryWritable<TMessage>
        => SendMessage(in EmptyMessage.Instance, in message);

    public void SendMessage<THeader, TMessage>(in THeader header, in TMessage message)
        where THeader : IBinaryWritable<THeader>
        where TMessage : IBinaryWritable<TMessage>
    {
        Session.SendMessage(
            Socket,
            new CommonHeader()
            {
                Type = MessageType.Session,
                ChannelId = ChannelId
            },
            in header,
            in message
        );
    }

    void IDisposable.Dispose()
        => Dispose();

    public void Dispose(bool closeSession = false, bool closeSocket = false)
    {
        MessageReceived = null;

        _handler.UnregisterChannel(this);
        if (closeSocket)
            Socket.Dispose(); // ToDo: Heartbeat!
        if (closeSession)
            Session.Dispose(); // ToDo: Heartbeat!
    }

    internal static CdpChannel CreateServerChannel(ChannelHandler handler, CdpSocket socket, ulong channelId)
        => new(handler, channelId, socket);

    internal static CdpChannel CreateClientChannel(ChannelHandler handler, CdpSocket socket, StartChannelResponse response)
        => new(handler, response.ChannelId, socket);
}
