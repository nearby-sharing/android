using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

/// <summary>
/// General payload to represent a result. <br/>
/// <br/>
/// (e.g. <see cref="Connection.ConnectionType.AuthDoneRespone"/>)
/// </summary>
public sealed class ResultPayload : ICdpPayload<ResultPayload>
{
    public static ResultPayload Parse(BinaryReader reader)
        => new()
        {
            Result = (CdpResult)reader.ReadByte()
        };

    public required CdpResult Result { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Result);
    }

    public void ThrowOnError()
    {
        if (Result != CdpResult.Success && Result != CdpResult.Pending)
            throw new CdpProtocolException($"Result indicating failure: {Result}");
    }
}