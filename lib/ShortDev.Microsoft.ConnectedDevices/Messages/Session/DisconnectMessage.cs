namespace ShortDev.Microsoft.ConnectedDevices.Messages.Session;

/// <summary>
/// The Disconnect message is an optional message sent by a client or host used to inform the other device to disconnect the connected session. <br/>
/// The <see cref="SessionId"/> is sent to identify the session to be disconnected.
/// </summary>
public sealed class DisconnectMessage : IBinaryParsable<DisconnectMessage>
{
    /// <summary>
    /// ID representing the session.
    /// </summary>
    public required ulong SessionId { get; init; }

    public static DisconnectMessage Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            SessionId = reader.ReadUInt64()
        };

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write(SessionId);
    }
}
