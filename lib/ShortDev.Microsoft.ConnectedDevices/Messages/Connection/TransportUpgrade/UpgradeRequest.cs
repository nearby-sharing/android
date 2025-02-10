namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

/// <summary>
/// This message transports the upgrade request.
/// </summary>
public sealed class UpgradeRequest : IBinaryWritable, IBinaryParsable<UpgradeRequest>
{
    /// <summary>
    /// A random GUID identifying this upgrade process across transports.
    /// </summary>
    public required Guid UpgradeId { get; init; }
    public required IReadOnlyList<EndpointMetadata> Endpoints { get; init; }

    public static UpgradeRequest Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            UpgradeId = reader.ReadGuid(),
            Endpoints = EndpointMetadata.ParseArray(ref reader)
        };

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write(UpgradeId);
        EndpointMetadata.WriteArray(ref writer, Endpoints);
    }
}