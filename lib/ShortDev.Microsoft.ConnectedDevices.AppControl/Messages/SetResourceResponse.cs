namespace ShortDev.Microsoft.ConnectedDevices.AppControl.Messages;

public readonly struct SetResourceResponse() : IBinaryWritable<SetResourceResponse>, IBinaryParsable<SetResourceResponse>
{
    public static SetResourceResponse Parse<TReader>(ref TReader reader)
        where TReader : struct, IEndianReader, allows ref struct
    {
        var hresult = reader.ReadUInt32();

        byte[] data = new byte[reader.ReadUInt32()];
        reader.ReadBytes(data);

        return new()
        {
            HResult = hresult,
            ResourceData = data,
        };
    }

    /// <summary>
    /// An HRESULT, where zero (0x00000000) is returned for successfully setting
    /// the resource data for the specific resource ID on the application app service.
    /// </summary>
    public required uint HResult { get; init; }

    /// <summary>
    /// An implementation-specific optional serialized BOND.NET response for the set resource request.
    /// </summary>
    public required ReadOnlyMemory<byte> ResourceData { get; init; }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write(HResult);
        writer.Write((uint)ResourceData.Length);
        writer.Write(ResourceData.Span);
    }
}
