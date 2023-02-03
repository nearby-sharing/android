using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Networking;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

/// <summary>
/// Provides the interface between a <see cref="CdpAppBase"/> and a <see cref="CdpSession"/>. <br/>
/// Every app has a unique <see cref="CdpSocket"/> managed from within this channel.
/// </summary>
public sealed class CdpChannel : IDisposable
{
    internal CdpChannel(CdpSession session, ulong channelId, CdpAppBase app, CdpSocket socket)
    {
        Session = session;
        ChannelId = channelId;
        App = app;
        app.Channel = this;
        Socket = socket;
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
        CommonHeader newHeader = new()
        {
            SequenceNumber = header.SequenceNumber + 1,
            Type = MessageType.Ack
        };
        newHeader.SetReplyToId(header.RequestID);

        EndianWriter writer = new(Endianness.BigEndian);
        newHeader.Write(writer);
        writer.CopyTo(Socket.Writer);
    }

    public void SendMessage(CommonHeader oldHeader, BodyCallback bodyCallback)
    {
        if (Session.Cryptor == null)
            throw new InvalidOperationException("Invalid session state!");

        lock (this)
        {
            CommonHeader header = new()
            {
                Type = MessageType.Session,

                SessionId = Session.GetSessionId(isHost: true),
                ChannelId = ChannelId,

                SequenceNumber = ++oldHeader.SequenceNumber
            };
            // ToDo: "AdditionalHeaders" ... "RequestID" ??

            EndianWriter writer = new(Endianness.BigEndian);
            Session.Cryptor.EncryptMessage(writer, header, bodyCallback);
            writer.CopyTo(Socket.Writer);
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
