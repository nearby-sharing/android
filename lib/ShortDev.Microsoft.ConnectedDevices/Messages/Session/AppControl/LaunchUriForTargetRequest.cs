namespace ShortDev.Microsoft.ConnectedDevices.Messages.Session.AppControl;

public sealed class LaunchUriForTargetRequest : IBinaryWritable, IBinaryParsable<LaunchUriForTargetRequest>
{
    public static LaunchUriForTargetRequest Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            Uri = reader.ReadStringWithLength(),
            LaunchLocation = (LaunchLocation)reader.ReadInt16(),
            RequestID = reader.ReadInt64(),
            PackageId = reader.ReadStringWithLength(),
            InstanceId = reader.ReadInt16(),
            AlternateId = reader.ReadStringWithLength(),
            TitleId = reader.ReadInt32(),
            FacadeName = reader.ReadStringWithLength(),
            InputData = reader.ReadBytesWithLength()
        };

    /// <summary>
    /// Uri to launch on remote device.
    /// </summary>
    public required string Uri { get; init; }
    public required LaunchLocation LaunchLocation { get; init; }
    /// <summary>
    /// A 64-bit arbitrary number identifying the request. <br/>
    /// The response ID in the response payload can then be used to correlate responses to requests.
    /// </summary>
    public required long RequestID { get; init; }
    /// <summary>
    /// The ID of the package of the app that hosts the app service.
    /// </summary>
    public required string PackageId { get; init; }
    /// <summary>
    /// The ID of the instance.
    /// </summary>
    public required short InstanceId { get; init; }
    /// <summary>
    /// The alternate ID of the package of the app that hosts the app service.
    /// </summary>
    public required string AlternateId { get; init; }
    /// <summary>
    /// The ID of the Title.
    /// </summary>
    public required int TitleId { get; init; }
    /// <summary>
    /// The name of the Facade.
    /// </summary>
    public required string FacadeName { get; init; }
    /// <summary>
    /// BOND.NET serialized data that is passed as a value set to the app launched by the call. <br/>
    /// (Optional)
    /// </summary>
    public ReadOnlyMemory<byte> InputData { get; init; } = default;

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.WriteWithLength(Uri);
        writer.Write((short)LaunchLocation);
        writer.Write(RequestID);
        writer.WriteWithLength(PackageId);
        writer.Write(InstanceId);
        writer.WriteWithLength(AlternateId);
        writer.Write(TitleId);
        writer.WriteWithLength(FacadeName);
        writer.WriteWithLength(InputData.Span);
    }
}
