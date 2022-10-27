using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Networking;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.Authentication;

public sealed class AuthenticationPayload : ICdpPayload<AuthenticationPayload>
{
    private AuthenticationPayload() { }

    public static AuthenticationPayload Parse(BinaryReader reader)
        => new()
        {
            Certificate = new(reader.ReadBytesWithLength()),
            SignedThumbprint = reader.ReadBytesWithLength()
        };

    public static AuthenticationPayload Create(X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
        => new()
        {
            Certificate = cert,
            SignedThumbprint = CreateSignedThumbprint(cert, hostNonce, clientNonce)
        };

    public required X509Certificate2 Certificate { get; init; }
    public required byte[] SignedThumbprint { get; init; }    

    public void Write(BinaryWriter writer)
    {
        writer.WriteWithLength(Certificate.Export(X509ContentType.Cert));
        writer.WriteWithLength(SignedThumbprint);
    }

    public bool VerifyThumbprint(CdpNonce hostNonce, CdpNonce clientNonce)
    {
        var publicKey = Certificate.GetECDsaPublicKey() ?? throw new InvalidDataException("Invalid certificate!");
        return publicKey.VerifyData(MergeNoncesWithCertificate(Certificate, hostNonce, clientNonce), SignedThumbprint, thumbprintHashType);
    }

    #region Thumbprint Api
    static readonly HashAlgorithmName thumbprintHashType = HashAlgorithmName.SHA256;

    static byte[] MergeNoncesWithCertificate(X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
    {
        byte[] certData = cert.Export(X509ContentType.Cert);
        using (MemoryStream stream = new())
        using (BinaryWriter writer = new(stream)) // Use little-endian here!
        {
            writer.Write(hostNonce.Value);
            writer.Write(clientNonce.Value);
            writer.Write(certData);
            return stream.ToArray();
        }
    }

    static byte[] CreateSignedThumbprint(X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
    {
        byte[] data = MergeNoncesWithCertificate(cert, hostNonce, clientNonce);
        var privateKey = cert.GetECDsaPrivateKey() ?? throw new ArgumentException("No ECDsa private key!", nameof(cert));
        return privateKey.SignData(data, thumbprintHashType);
    }
    #endregion
}
