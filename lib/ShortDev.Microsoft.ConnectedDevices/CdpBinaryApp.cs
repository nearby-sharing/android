using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
using ShortDev.Microsoft.ConnectedDevices.Serialization;

namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// Represents an application layer that sends arbitrary binary messages.
/// </summary>
public abstract class CdpBinaryApp(CdpChannel channel) : CdpAppBase(channel)
{
    public sealed override void HandleMessage(CdpMessage msg)
    {
        msg.Read(out var reader);

        var header = BinaryMsgHeader.Parse(ref reader); // ToDo: What about fragmentation?
        HandleMessage(in header, ref reader);
    }

    /// <summary>
    /// Handles the received message.
    /// </summary>
    /// <param name="reader">Reader on the received binary data</param>
    protected abstract void HandleMessage<TReader>(in BinaryMsgHeader header, ref TReader reader)
        where TReader : struct, IEndianReader, allows ref struct;

    protected void SendBinaryMessage<TMessage>(in TMessage message, uint messageId)
        where TMessage : IBinaryWritable<TMessage>
    {
        Channel.SendMessage(
            new BinaryMsgHeader()
            {
                MessageId = messageId,
            },
            in message
        );
    }
}

/// <summary>
/// Represents an application layer that sends Bond.Net serialized <see cref="ValueSet"/>s.
/// </summary>
public abstract class CdpBondApp(CdpChannel channel) : CdpBinaryApp(channel)
{
    protected override void HandleMessage<TReader>(in BinaryMsgHeader header, ref TReader reader)
    {
        var message = ValueSet.Parse(ref reader);
        HandleMessage(in header, message);
    }

    protected abstract void HandleMessage(in BinaryMsgHeader header, ValueSet message);

    protected void SendValueSet(ValueSet request, uint messageId)
        => SendBinaryMessage(request, messageId);
}
