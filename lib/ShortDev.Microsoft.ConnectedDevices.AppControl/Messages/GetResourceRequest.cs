using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.AppControl.Messages;

public readonly struct GetResourceRequest() : IBinaryWritable<GetResourceRequest>, IBinaryParsable<GetResourceRequest>
{
    public static GetResourceRequest Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new() { ResourceUrl = reader.ReadStringWithLength(Encoding.UTF8) };

    /// <summary>
    /// The URL that represents the application instance ID and the resource ID.Conforms to &lt;app id&gt;/&lt;resource id&gt;.
    /// </summary>
    public required string ResourceUrl { get; init; }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.WriteWithLength(ResourceUrl, Encoding.UTF8);
    }
}
