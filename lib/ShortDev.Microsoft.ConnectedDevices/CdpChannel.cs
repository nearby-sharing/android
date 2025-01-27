using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
using ShortDev.Microsoft.ConnectedDevices.Session.Channels;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Buffers;

namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// Provides the interface between a <see cref="CdpAppBase"/> and a <see cref="CdpSession"/>.
/// </summary>
public sealed class CdpChannel : IDisposable
{
    readonly ChannelHandler _handler;
    private CdpChannel(ChannelHandler handler, ulong channelId, CdpSocket socket, CdpAppBase app)
    {
        _handler = handler;

        Session = handler.Session;
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

    public void SendBinaryMessage(BodyCallback bodyCallback, uint msgId)
    {
        CommonHeader header = new()
        {
            Type = MessageType.Session,
            ChannelId = ChannelId
        };

        using var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
        new BinaryMsgHeader()
        {
            MessageId = msgId
        }.Write(writer);
        bodyCallback(writer);

        using SpeedMeassure speedMeassure = new((uint)writer.Buffer.WrittenSpan.Length);
        Session.SendMessage(Socket, header, writer);
    }

    void IDisposable.Dispose()
        => Dispose();

    public void Dispose(bool closeSession = false, bool closeSocket = false)
    {
        _handler.UnregisterChannel(this);
        if (closeSocket)
            Socket.Dispose(); // ToDo: Heartbeat!
        if (closeSession)
            Session.Dispose(); // ToDo: Heartbeat!
    }

    internal static CdpChannel CreateServerChannel(ChannelHandler handler, CdpSocket socket, StartChannelRequest request, ulong channelId)
    {
        var app = CdpAppRegistration.InstantiateApp(request.Id, request.Name, handler.Session.Platform);
        CdpChannel channel = new(handler, channelId, socket, app);
        app.Initialize(channel);
        return channel;
    }

    internal static CdpChannel CreateClientChannel(ChannelHandler handler, CdpSocket socket, StartChannelResponse response, CdpAppBase app)
    {
        CdpChannel channel = new(handler, response.ChannelId, socket, app);
        app.Initialize(channel);
        return channel;
    }
}
