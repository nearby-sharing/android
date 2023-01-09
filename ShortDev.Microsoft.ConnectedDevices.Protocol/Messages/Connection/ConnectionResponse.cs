using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Networking;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Messages.Connection;

/// <summary>
/// The host responds with a connection response message including device information. <br/>
/// Only the Result is sent if the Result is anything other than <see cref="ConnectionResult.Pending"/>.
/// </summary>
public sealed class ConnectionResponse : ICdpPayload<ConnectionResponse>
{
    /// <summary>
    /// The result of the connection request.
    /// </summary>
    public required ConnectionResult Result { get; set; }
    /// <summary>
    /// The expected size of HMAC.
    /// </summary>
    public required ushort HmacSize { get; set; }
    /// <summary>
    /// Random values.
    /// </summary>
    public required CdpNonce Nonce { get; set; }
    /// <summary>
    /// The maximum size of a single message fragment. <br/>
    /// (Fixed Value of <see cref="Constants.DefaultMessageFragmentSize"/>).
    /// </summary>
    public required uint MessageFragmentSize { get; set; }
    /// <summary>
    /// A fixed-length key that is based on the <see cref="CurveType"/> from <see cref="ConnectionRequest"/>, which is sent only if the connection is successful. <br/>
    /// This is the X component of the key. <br/>
    /// (See <see cref="System.Security.Cryptography.ECPoint.X"/>)
    /// </summary>
    public required byte[] PublicKeyX { get; set; }
    /// <summary>
    /// A fixed-length key that is based on the <see cref="CurveType"/> from <see cref="ConnectionRequest"/>, which is sent only if the connection is successful. <br/>
    /// This is the Y component of the key.
    /// (See <see cref="System.Security.Cryptography.ECPoint.Y"/>)
    /// </summary>
    public required byte[] PublicKeyY { get; set; }

    public static ConnectionResponse Parse(BinaryReader reader)
    {
        var result = (ConnectionResult)reader.ReadByte();
        return new()
        {
            Result = result,
            HmacSize = reader.ReadUInt16(),
            Nonce = new(reader.ReadUInt16()),
            MessageFragmentSize = reader.ReadUInt32(),
            PublicKeyX = reader.ReadBytesWithLength(),
            PublicKeyY = reader.ReadBytesWithLength()
        };
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Result);
        writer.Write((ushort)HmacSize);
        writer.Write(Nonce.Value);
        writer.Write((uint)MessageFragmentSize);

        writer.Write((ushort)PublicKeyX.Length);
        writer.Write(PublicKeyX);
        writer.Write((ushort)PublicKeyY.Length);
        writer.Write(PublicKeyY);
    }
}
