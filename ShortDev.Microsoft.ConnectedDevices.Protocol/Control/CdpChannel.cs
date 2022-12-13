using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Control;

/// <summary>
/// Provides the interface between a <see cref="CdpAppBase"/> and a <see cref="CdpSession"/>. <br/>
/// Every app has a unique <see cref="CdpSocket"/> managed from within this channel.
/// </summary>
public sealed class CdpChannel : IDisposable
{
    BinaryWriter _writer;
    internal CdpChannel(CdpSession session, ulong channelId, CdpAppBase app, CdpSocket socket)
    {
        Session = session;
        ChannelId = channelId;
        App = app;
        app.Channel = this;
        Socket = socket;
        _writer = socket.Writer;
    }

    /// <summary>
    /// Get's the corresponding <see cref="CdpSession"/>. <br/>
    /// <inheritdoc cref="CdpSession"/>
    /// </summary>
    public CdpSession Session { get; }

    /// <summary>
    /// Get's the corresponding <see cref="CdpSocket"/>. <br/>
    /// <inheritdoc cref="CdpSocket" />
    /// </summary>
    public CdpSocket Socket { get; }

    /// <summary>
    /// Get's the corresponding <see cref="CdpAppBase"/>. <br/>
    /// <inheritdoc cref="CdpAppBase"/>
    /// </summary>
    public CdpAppBase App { get; }

    /// <summary>
    /// Get's the unique id for the channel. <br/>
    /// The id is unique as long as the channel is active.
    /// </summary>
    public ulong ChannelId { get; }

    public async ValueTask HandleMessageAsync(CdpMessage msg)
        => await App.HandleMessageAsync(msg);

    public void SendAck(CommonHeader header)
    {
        CommonHeader newHeader = new();
        newHeader.SequenceNumber = header.SequenceNumber + 1;
        newHeader.Type = MessageType.Ack;
        newHeader.SetReplyToId(header.RequestID);
        newHeader.Write(_writer);
    }

    public void SendMessage(CommonHeader oldHeader, Action<BinaryWriter> bodyCallback)
    {
        if (Session.Cryptor == null)
            throw new InvalidOperationException("Invalid session state!");

        lock (_writer)
        {
            CommonHeader header = new();
            header.Type = MessageType.Session;

            header.SessionId = Session.GetSessionId(isHost: true);
            header.ChannelId = ChannelId;

            header.SequenceNumber = ++oldHeader.SequenceNumber;
            // ToDo: "AdditionalHeaders" ... "RequestID" ??

            Session.Cryptor.EncryptMessage(_writer, header, bodyCallback);
            _writer.Flush();
        }
    }

    void IDisposable.Dispose()
        => Dispose();

    public void Dispose(bool closeSession = false, bool closeSocket = false)
    {
        Session.UnregisterChannel(this);
        App.Dispose();
        if (closeSocket)
            Socket.Dispose();
        if (closeSession)
            Session.Dispose();
    }
}
