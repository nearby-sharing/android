using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using System;

namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// Provides the interface between a <see cref="IChannelMessageHandler"/> and a <see cref="CdpSession"/>. <br/>
/// Every handler / app has a unique <see cref="CdpSocket"/> managed from within this channel.
/// </summary>
public sealed class CdpChannel : IDisposable
{
    internal CdpChannel(CdpSession session, ulong channelId, IChannelMessageHandler handler, CdpSocket socket)
    {
        Session = session;
        ChannelId = channelId;
        MessageHandler = handler;
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
    /// Get's the corresponding <see cref="IChannelMessageHandler"/>. <br/>
    /// (See <see cref="CdpAppBase"/>)
    /// </summary>
    public IChannelMessageHandler MessageHandler { get; }

    /// <summary>
    /// Get's the unique id for the channel. <br/>
    /// The id is unique as long as the channel is active.
    /// </summary>
    public ulong ChannelId { get; }

    public void HandleMessageAsync(CdpMessage msg)
        => MessageHandler.HandleMessage(msg);

    public void SendMessage(BodyCallback bodyCallback)
    {
        CommonHeader header = new()
        {
            Type = MessageType.Session,
            ChannelId = ChannelId
        };

        Session.SendMessage(Socket, header, writer =>
        {
            new SessionFragmentHeader()
            {
                MessageId = 0
            }.Write(writer);
            bodyCallback(writer);
        });
    }

    void IDisposable.Dispose()
        => Dispose();

    public void Dispose(bool closeSession = false, bool closeSocket = false)
    {
        Session.UnregisterChannel(this);
        if (closeSocket)
            Socket.Dispose();
        if (closeSession)
            Session.Dispose();
    }
}
