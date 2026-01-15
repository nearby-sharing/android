namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

/// <summary>
/// This message transports the details of the upgrade. <br/>
/// (See <see cref="ConnectionType.TransportRequest"/> and <see cref="ConnectionType.TransportConfirmation"/>)
/// </summary>
public readonly record struct UpgradeIdPayload : IBinaryWritable, IBinaryParsable<UpgradeIdPayload>
{
    public required Guid UpgradeId { get; init; }

    public static UpgradeIdPayload Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            UpgradeId = reader.ReadGuid()
        };

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write(UpgradeId);
    }
}
