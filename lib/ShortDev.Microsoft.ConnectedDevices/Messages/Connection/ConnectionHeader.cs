namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection;

/// <summary>
/// The <see cref="ConnectionHeader"/> is common for all Connection Messages.
/// </summary>
public readonly record struct ConnectionHeader : IBinaryWritable, IBinaryParsable<ConnectionHeader>
{
    /// <summary>
    /// Indicates the current connection type.
    /// </summary>
    public required ConnectionType MessageType { get; init; }

    /// <summary>
    /// Displays the types of available connections.
    /// </summary>
    public required ConnectionMode ConnectionMode { get; init; }

    public static ConnectionHeader Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        return new()
        {
            ConnectionMode = (ConnectionMode)reader.ReadInt16(),
            MessageType = (ConnectionType)reader.ReadUInt8()
        };
    }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write((short)ConnectionMode);
        writer.Write((byte)MessageType);
    }
}
