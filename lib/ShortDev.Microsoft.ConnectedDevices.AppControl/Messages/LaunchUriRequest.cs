namespace ShortDev.Microsoft.ConnectedDevices.AppControl.Messages;

public readonly record struct LaunchUriRequest() : IBinaryWritable<LaunchUriRequest>, IBinaryParsable<LaunchUriRequest>
{
    public static LaunchUriRequest Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        var uri = reader.ReadStringWithLength();

        var launchLocation = (LaunchLocation)reader.ReadUInt16();
        var requestID = reader.ReadUInt64();

        byte[] data = new byte[reader.ReadUInt32()];
        reader.ReadBytes(data);

        return new LaunchUriRequest
        {
            Uri = uri,
            LaunchLocation = launchLocation,
            RequestID = requestID,
            InputData = data,
        };
    }

    /// <summary>
    /// Uri to launch on remote device.
    /// </summary>
    public required string Uri { get; init; }
    public required LaunchLocation LaunchLocation { get; init; }
    /// <summary>
    /// A 64-bit arbitrary number identifying the request. <br/>
    /// The response ID in the response payload can then be used to correlate responses to requests.
    /// </summary>
    public required ulong RequestID { get; init; }

    /// <summary>
    /// BOND.NET serialized data that is passed as a value set to the app launched by the call. <br/>
    /// (Optional)
    /// </summary>
    public ReadOnlyMemory<byte> InputData { get; init; }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.WriteWithLength(Uri);
        writer.Write((short)LaunchLocation);
        writer.Write(RequestID);
        writer.Write((uint)InputData.Length);
        writer.Write(InputData.Span);
    }
}
