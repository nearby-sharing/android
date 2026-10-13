using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.AppControl.Messages;

public readonly struct SetResourceRequest() : IBinaryWritable<SetResourceRequest>, IBinaryParsable<SetResourceRequest>
{
    public static SetResourceRequest Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        var url = reader.ReadStringWithLength();

        byte[] data = new byte[reader.ReadUInt32()];
        reader.ReadBytes(data);

        return new()
        {
            ResourceUrl = url,
            ResourceData = data
        };
    }

    /// <summary>
    /// The URL that represents the application instance ID and the resource ID.Conforms to &lt;app id&gt;/&lt;resource id&gt;.
    /// </summary>
    public required string ResourceUrl { get; init; }

    /// <summary>
    /// The UTF-8-encoded resource data to be set on the application app service.
    /// </summary>
    public required ReadOnlyMemory<byte> ResourceData { get; init; }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.WriteWithLength(ResourceUrl, Encoding.UTF8);
        writer.Write((uint)ResourceData.Length);
        writer.Write(ResourceData.Span);
    }
}
