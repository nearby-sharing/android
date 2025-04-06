namespace ShortDev.Microsoft.ConnectedDevices.Messages.Session.AppControl;

public readonly record struct LaunchUriRequest() : IBinaryWritable, IBinaryParsable<LaunchUriRequest>
{
    public static LaunchUriRequest Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            Uri = reader.ReadStringWithLength(),
            LaunchLocation = (LaunchLocation)reader.ReadInt16(),
            RequestID = reader.ReadInt64(),
            InputData = reader.ReadStringWithLength() // ToDo: Optional??
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
    /// BOND.NET serialized data that is passed as a value set to the app launched by the call. <br/>
    /// (Optional)
    /// </summary>
    public string InputData { get; init; } = string.Empty;

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.WriteWithLength(Uri);
        writer.Write((short)LaunchLocation);
        writer.Write(RequestID);
        writer.WriteWithLength(InputData);
    }
}
