using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Networking;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.Authentication;

public sealed class DeviceAuthenticationMessage : ICdpPayload<DeviceAuthenticationMessage>
{
    public static DeviceAuthenticationMessage Parse(BinaryReader reader)
        => new()
        {
            DeviceCert = new(reader.ReadBytesWithLength()),
            SignedThumbprint = reader.ReadBytesWithLength()
        };

    public static DeviceAuthenticationMessage FromCertificate(X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
        => new()
        {
            DeviceCert = cert,
            SignedThumbprint = CreateSignedThumbprintFromCertificate(cert, hostNonce, clientNonce)
        };

    public required X509Certificate2 DeviceCert { get; init; }
    public required byte[] SignedThumbprint { get; init; }


    #region Thumbprint
    static readonly HashAlgorithmName thumbprintHashType = HashAlgorithmName.SHA256;
    static byte[] CreateSignedThumbprintFromCertificate(X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
    {
        byte[] data = MergeNoncesWithCertificate(cert, hostNonce, clientNonce);
        var privateKey = cert.GetECDsaPrivateKey() ?? throw new ArgumentException("No ECDsa private key!", nameof(cert));
        return privateKey.SignData(data, thumbprintHashType);
    }
    #endregion

    static byte[] MergeNoncesWithCertificate(X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
    {
        byte[] certData = cert.Export(X509ContentType.Cert);
        using (MemoryStream stream = new())
        using (BinaryWriter writer = new(stream))
        {
            writer.Write(hostNonce.Value.Reverse());
            writer.Write(clientNonce.Value.Reverse());
            writer.Write(certData);
            return stream.ToArray();
        }
    }

    public bool VerifyThumbprint(CdpNonce hostNonce, CdpNonce clientNonce)
    {
        var publicKey = DeviceCert.GetECDsaPublicKey() ?? throw new InvalidDataException("Invalid certificate!");
        return publicKey.VerifyData(MergeNoncesWithCertificate(DeviceCert, hostNonce, clientNonce), SignedThumbprint, thumbprintHashType);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteWithLength(DeviceCert.Export(X509ContentType.Cert));
        writer.WriteWithLength(SignedThumbprint);
    }
}
