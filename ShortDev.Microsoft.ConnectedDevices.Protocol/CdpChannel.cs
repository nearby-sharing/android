using System;
using System.IO;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

public sealed class CdpChannel : IDisposable
{
    BinaryWriter _writer;
    internal CdpChannel(CdpSession session, ulong channelId, ICdpApp app, BinaryWriter writer)
    {
        Session = session;
        ChannelId = channelId;
        App = app;
        _writer = writer;
    }

    public CdpSession Session { get; }
    public ICdpApp App { get; }
    public ulong ChannelId { get; }

    public async ValueTask HandleMessageAsync(CdpMessage msg)
        => await App.HandleMessageAsync(this, msg);

    public void SendAck(CommonHeader header)
    {
        CommonHeader newHeader = new();
        newHeader.SequenceNumber = header.SequenceNumber + 1;
        newHeader.Type = MessageType.Ack;
        newHeader.SetReplyToId(header.RequestID);
        newHeader.Write(_writer);
    }

    public void SendMessage(CommonHeader header, Action<BinaryWriter> bodyCallback)
    {
        if (Session._cryptor == null)
            throw new InvalidOperationException("Invalid session state!");

        header.RequestID++;
        header.SequenceNumber++;
        Session._cryptor.EncryptMessage(_writer, header, bodyCallback);
    }

    public void Dispose()
    {
        lock (Session._channelRegistry)
        {
            Session._channelRegistry.Remove(ChannelId);
        }
        App.Dispose();
    }
}
