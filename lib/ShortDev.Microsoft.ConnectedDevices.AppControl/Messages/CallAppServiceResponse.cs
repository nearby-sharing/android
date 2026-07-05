namespace ShortDev.Microsoft.ConnectedDevices.AppControl.Messages;

public readonly struct CallAppServiceResponse() : IBinaryWritable<CallAppServiceResponse>, IBinaryParsable<CallAppServiceResponse>
{
    public static CallAppServiceResponse Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        var hresult = reader.ReadUInt32();

        byte[] data = new byte[reader.ReadUInt32()];
        reader.ReadBytes(data);

        return new CallAppServiceResponse
        {
            HResult = hresult,
            Data = data
        };
    }

    /// <summary>
    /// The HRESULT returned by the call, zero (0x00000000) if successful.
    /// </summary>
    public required uint HResult { get; init; }

    public bool IsSuccess => HResult == 0;

    /// <summary>
    /// The UTF-8-encoded response returned from the application app service.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; init; }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        throw new NotImplementedException();
    }
}
