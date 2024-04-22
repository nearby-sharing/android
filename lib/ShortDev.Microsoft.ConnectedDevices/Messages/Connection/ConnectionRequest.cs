﻿using ShortDev.Microsoft.ConnectedDevices.Encryption;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection;

/// <summary>
/// Client initiates a connection request with a host device.
/// </summary>
public sealed class ConnectionRequest : ICdpPayload<ConnectionRequest>
{
    /// <summary>
    /// The type of elliptical curve used.
    /// </summary>
    public required CurveType CurveType { get; set; }
    /// <summary>
    /// The expected size of HMAC.
    /// </summary>
    public required ushort HmacSize { get; set; }
    /// <summary>
    /// Random values
    /// </summary>
    public required CdpNonce Nonce { get; set; }
    /// <summary>
    /// The maximum size of a single message fragment. <br/>
    /// (Fixed Value of <see cref="Constants.DefaultMessageFragmentSize"/>).
    /// </summary>
    public required uint MessageFragmentSize { get; set; }
    /// <summary>
    /// A fixed-length key.
    /// This is the X component of the key. <br/>
    /// (See <see cref="System.Security.Cryptography.ECPoint.X"/>)
    /// </summary>
    public required byte[] PublicKeyX { get; set; }
    /// <summary>
    /// A fixed-length key.
    /// This is the Y component of the key. <br/>
    /// (See <see cref="System.Security.Cryptography.ECPoint.Y"/>)
    /// </summary>
    public required byte[] PublicKeyY { get; set; }

    public static ConnectionRequest Parse(ref EndianReader reader)
        => new()
        {
            CurveType = (CurveType)reader.ReadByte(),
            HmacSize = reader.ReadUInt16(),
            Nonce = new(reader.ReadUInt64()),
            MessageFragmentSize = reader.ReadUInt32(),
            PublicKeyX = reader.ReadBytesWithLength().ToArray(),
            PublicKeyY = reader.ReadBytesWithLength().ToArray()
        };

    public void Write(EndianWriter writer)
    {
        writer.Write((byte)CurveType);
        writer.Write((ushort)HmacSize);
        writer.Write(Nonce.Value);
        writer.Write((uint)MessageFragmentSize);

        writer.Write((ushort)PublicKeyX.Length);
        writer.Write(PublicKeyX);
        writer.Write((ushort)PublicKeyY.Length);
        writer.Write(PublicKeyY);
    }
}