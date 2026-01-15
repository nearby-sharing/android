using ShortDev.IO.ValueStream;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.Authentication;

/// <summary>
/// For all authentication, devices send their device / user certificate, which is self-signed.
/// </summary>
public readonly record struct AuthenticationPayload : IBinaryWritable, IBinaryParsable<AuthenticationPayload>
{
    public static AuthenticationPayload Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            Certificate = reader.ReadCert(),
            SignedThumbprint = reader.ReadBytesWithLength()
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
    public required ReadOnlyMemory<byte> SignedThumbprint { get; init; }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.WriteWithLength(Certificate.Export(X509ContentType.Cert));
        writer.WriteWithLength(SignedThumbprint.Span);
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

        // Use little-endian here!
        using var nonceWriter = EndianWriter.Create(Endianness.LittleEndian, ConnectedDevicesPlatform.MemoryPool);
        MergeNoncesWithCertificate(nonceWriter, Certificate, hostNonce, clientNonce);

        return publicKey.VerifyData(nonceWriter.Stream.WrittenSpan, SignedThumbprint.Span, thumbprintHashType);
    }

    #region Thumbprint Api
    static readonly HashAlgorithmName thumbprintHashType = HashAlgorithmName.SHA256;

    static void MergeNoncesWithCertificate(EndianWriter<HeapOutputStream> writer, X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
    {
        byte[] certData = cert.Export(X509ContentType.Cert);

        writer.Write(hostNonce.Value);
        writer.Write(clientNonce.Value);
        writer.Write(certData);
    }

    static byte[] CreateSignedThumbprint(X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
    {
        // Use little-endian here!
        using var nonceWriter = EndianWriter.Create(Endianness.LittleEndian, ConnectedDevicesPlatform.MemoryPool);
        MergeNoncesWithCertificate(nonceWriter, cert, hostNonce, clientNonce);

        var privateKey = cert.GetECDsaPrivateKey() ?? throw new ArgumentException("No ECDsa private key!", nameof(cert));
        return privateKey.SignData(nonceWriter.Stream.WrittenSpan, thumbprintHashType);
    }
    #endregion
}
