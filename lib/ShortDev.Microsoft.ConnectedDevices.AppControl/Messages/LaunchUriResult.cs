namespace ShortDev.Microsoft.ConnectedDevices.AppControl.Messages;

public readonly record struct LaunchUriResult : IBinaryWritable<LaunchUriResult>, IBinaryParsable<LaunchUriResult>
{
    public static LaunchUriResult Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        var result = reader.ReadUInt32();
        var responseId = reader.ReadUInt64();

        byte[] data = new byte[reader.ReadUInt32()];
        reader.ReadBytes(data);

        return new LaunchUriResult
        {
            Result = result,
            ResponseID = responseId,
            Data = data
        };
    }

    /// <summary>
    /// The HRESULT returned by the call, zero if successful.
    /// </summary>
    public required uint Result { get; init; }

    /// <summary>
    /// Number corresponding to the request ID from the Launch URI message that resulted in this response. <br/>
    /// This is used to correlate requests and responses
    /// </summary>
    public required ulong ResponseID { get; init; }

    /// <summary>
    /// Optional. BOND.NET serialized data that is passed as a value set from the app launched by the call.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; init; }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write(Result);
        writer.Write(ResponseID);
        writer.Write((uint)Data.Length);
        writer.Write(Data.Span);
    }
}
