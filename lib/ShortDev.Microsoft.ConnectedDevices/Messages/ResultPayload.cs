using ShortDev.Microsoft.ConnectedDevices.Exceptions;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

/// <summary>
/// General payload to represent a result. <br/>
/// <br/>
/// (e.g. <see cref="Connection.ConnectionType.AuthDoneRespone"/>)
/// </summary>
public sealed class ResultPayload : IBinaryWritable, IBinaryParsable<ResultPayload>
{
    public static ResultPayload Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            Result = (CdpResult)reader.ReadUInt8()
        };

    public required CdpResult Result { get; init; }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write((byte)Result);
    }

    public void ThrowOnError()
    {
        if (Result != CdpResult.Success && Result != CdpResult.Pending)
            throw new CdpProtocolException($"Result indicating failure: {Result}");
    }
}