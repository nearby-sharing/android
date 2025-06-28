using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection;

/// <summary>
/// Client initiates a connection request with a host device.
/// </summary>
public readonly record struct ConnectionRequest : IBinaryWritable, IBinaryParsable<ConnectionRequest>
{
    /// <summary>
    /// The type of elliptical curve used.
    /// </summary>
    public required CurveType CurveType { get; init; }
    /// <summary>
    /// The expected size of HMAC.
    /// </summary>
    public required ushort HmacSize { get; init; }
    /// <summary>
    /// Random values
    /// </summary>
    public required CdpNonce Nonce { get; init; }
    /// <summary>
    /// The maximum size of a single message fragment. <br/>
    /// (Fixed Value of <see cref="MessageFragmenter.DefaultMessageFragmentSize"/>).
    /// </summary>
    public required uint MessageFragmentSize { get; init; }
    /// <summary>
    /// A fixed-length key.
    /// This is the X component of the key. <br/>
    /// (See <see cref="System.Security.Cryptography.ECPoint.X"/>)
    /// </summary>
    public required ReadOnlyMemory<byte> PublicKeyX { get; init; }
    /// <summary>
    /// A fixed-length key.
    /// This is the Y component of the key. <br/>
    /// (See <see cref="System.Security.Cryptography.ECPoint.Y"/>)
    /// </summary>
    public required ReadOnlyMemory<byte> PublicKeyY { get; init; }

    public static ConnectionRequest Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            CurveType = (CurveType)reader.ReadUInt8(),
            HmacSize = reader.ReadUInt16(),
            Nonce = new(reader.ReadUInt64()),
            MessageFragmentSize = reader.ReadUInt32(),
            PublicKeyX = reader.ReadBytesWithLength(),
            PublicKeyY = reader.ReadBytesWithLength()
        };

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write((byte)CurveType);
        writer.Write((ushort)HmacSize);
        writer.Write(Nonce.Value);
        writer.Write((uint)MessageFragmentSize);

        writer.Write((ushort)PublicKeyX.Length);
        writer.Write(PublicKeyX.Span);
        writer.Write((ushort)PublicKeyY.Length);
        writer.Write(PublicKeyY.Span);
    }
}