using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using System;
using System.Collections.Generic;

namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// Provides the interface between a <see cref="CdpAppBase"/> and a <see cref="CdpSession"/>.
/// </summary>
public sealed class CdpChannel : IDisposable
{
    private CdpChannel(CdpSession session, ulong channelId, CdpSocket socket, CdpAppBase app)
    {
        Session = session;
        ChannelId = channelId;
        Socket = socket;
        App = app;
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
    /// Get's the corresponding <see cref="CdpAppBase"/>. <br/>
    /// <br/>
    /// <inheritdoc cref="CdpAppBase"/>
    /// </summary>
    public CdpAppBase App { get; private set; }

    /// <summary>
    /// Get's the unique id for the channel. <br/>
    /// The id is unique as long as the channel is active.
    /// </summary>
    public ulong ChannelId { get; }

    public void SendBinaryMessage(BodyCallback bodyCallback, uint msgId, List<AdditionalHeader>? headers = null)
    {
        CommonHeader header = new()
        {
            Type = MessageType.Session,
            ChannelId = ChannelId
        };

        if (headers != null)
            header.AdditionalHeaders = headers;

        Session.SendMessage(Socket, header, writer =>
        {
            new BinaryMsgHeader()
            {
                MessageId = msgId
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
            Socket.Dispose(); // ToDo: Heartbeat!
        if (closeSession)
            Session.Dispose(); // ToDo: Heartbeat!
    }

    internal static CdpChannel CreateServerChannel(CdpSession session, ulong channelId, CdpSocket socket, StartChannelRequest request)
    {
        var app = CdpAppRegistration.InstantiateApp(request.Id, request.Name, session.Platform);
        CdpChannel channel = new(session, channelId, socket, app);
        app.Initialize(channel);
        return channel;
    }

    internal static CdpChannel CreateClientChannel(CdpSession session, CdpSocket socket, StartChannelResponse response, CdpAppBase app)
    {
        CdpChannel channel = new(session, response.ChannelId, socket, app);
        app.Initialize(channel);
        return channel;
    }
}
