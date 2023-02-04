using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Networking;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.Authentication;

/// <summary>
/// For all authentication, devices send their device / user certificate, which is self-signed.
/// </summary>
public sealed class AuthenticationPayload : ICdpPayload<AuthenticationPayload>
{
    private AuthenticationPayload() { }

    public static AuthenticationPayload Parse(EndianReader reader)
        => new()
        {
            Certificate = new(reader.ReadBytesWithLength().ToArray()),
            SignedThumbprint = reader.ReadBytesWithLength().ToArray()
        };

    /// <summary>
    /// Creates a new <see cref="AuthenticationPayload"/> based on a self-signed device / user certificat and host and client <see cref="CdpNonce"/>.
    /// </summary>
    /// <param name="cert">Self signed certificat</param>
    /// <param name="hostNonce"><see cref="CdpNonce"/> of host device</param>
    /// <param name="clientNonce"><see cref="CdpNonce"/> of client device</param>
    /// <returns>The generated payload</returns>
    public static AuthenticationPayload Create(X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
        => new()
        {
            Certificate = cert,
            SignedThumbprint = CreateSignedThumbprint(cert, hostNonce, clientNonce)
        };

    /// <summary>
    /// A Device / User Certificate.
    /// </summary>
    public required X509Certificate2 Certificate { get; init; }
    /// <summary>
    /// A signed Device Cert Thumbprint.
    /// </summary>
    public required byte[] SignedThumbprint { get; init; }

    public void Write(EndianWriter writer)
    {
        writer.WriteWithLength(Certificate.Export(X509ContentType.Cert));
        writer.WriteWithLength(SignedThumbprint);
    }

    /// <summary>
    /// Checks wether the transmitted tumbprint is valid given both host and client <see cref="CdpNonce"/>.
    /// </summary>
    /// <param name="hostNonce"><see cref="CdpNonce"/> of host device</param>
    /// <param name="clientNonce"><see cref="CdpNonce"/> of client device</param>
    /// <returns>Wether the thumbprint of the current certificat is valid</returns>
    /// <exception cref="InvalidDataException"></exception>
    public bool VerifyThumbprint(CdpNonce hostNonce, CdpNonce clientNonce)
    {
        var publicKey = Certificate.GetECDsaPublicKey() ?? throw new CdpSecurityException("Invalid certificate!");

        var merged = MergeNoncesWithCertificate(Certificate, hostNonce, clientNonce).Span;
        return publicKey.VerifyData(merged, SignedThumbprint, thumbprintHashType);
    }

    #region Thumbprint Api
    static readonly HashAlgorithmName thumbprintHashType = HashAlgorithmName.SHA256;

    static ReadOnlyMemory<byte> MergeNoncesWithCertificate(X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
    {
        byte[] certData = cert.Export(X509ContentType.Cert);

        EndianWriter writer = new(Endianness.LittleEndian); // Use little-endian here!
        writer.Write(hostNonce.Value);
        writer.Write(clientNonce.Value);
        writer.Write(certData);
        return writer.Buffer.AsMemory();
    }

    static byte[] CreateSignedThumbprint(X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
    {
        var data = MergeNoncesWithCertificate(cert, hostNonce, clientNonce).ToArray();
        var privateKey = cert.GetECDsaPrivateKey() ?? throw new ArgumentException("No ECDsa private key!", nameof(cert));
        return privateKey.SignData(data, thumbprintHashType);
    }
    #endregion
}
