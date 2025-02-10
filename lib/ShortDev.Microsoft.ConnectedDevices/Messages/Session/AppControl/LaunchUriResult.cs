namespace ShortDev.Microsoft.ConnectedDevices.Messages.Session.AppControl;

public sealed class LaunchUriResult : IBinaryWritable, IBinaryParsable<LaunchUriResult>
{
    public static LaunchUriResult Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            Result = reader.ReadInt32(),
            ResponseID = reader.ReadInt32()
        };

    /// <summary>
    /// The HRESULT returned by the call, zero if successful.
    /// </summary>
    public required int Result { get; init; }

    /// <summary>
    /// Number corresponding to the request ID from the Launch URI message that resulted in this response. <br/>
    /// This is used to correlate requests and responses
    /// </summary>
    public required int ResponseID { get; init; }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write(Result);
        writer.Write(ResponseID);
    }
}
