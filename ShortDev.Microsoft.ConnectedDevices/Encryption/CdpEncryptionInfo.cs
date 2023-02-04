using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ShortDev.Microsoft.ConnectedDevices.Encryption;

public sealed class CdpEncryptionInfo
{
    public required CdpNonce Nonce { get; init; }
    public required ECDiffieHellman DiffieHellman { get; init; }
    public required CdpEncryptionParams EncryptionParams { get; init; }

    public X509Certificate2? DeviceCertificate { get; init; }

    public ECPoint PublicKey
        => DiffieHellman.ExportParameters(false).Q;

    public static CdpEncryptionInfo Create(CdpEncryptionParams encryptionParams)
    {
        CertificateRequest certRequest = new("CN=Ms-Cdp", ECDsa.Create(encryptionParams.Curve), HashAlgorithmName.SHA256);
        return new()
        {
            DiffieHellman = ECDiffieHellman.Create(encryptionParams.Curve),
            Nonce = CdpNonce.Create(),
            EncryptionParams = encryptionParams,
            DeviceCertificate = certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5))
        };
    }

    public static CdpEncryptionInfo FromRemote(byte[] publicX, byte[] publicY, CdpNonce nonce, CdpEncryptionParams encryptionParams)
    {
        var diffieHellman = ECDiffieHellman.Create(new ECParameters()
        {
            Curve = encryptionParams.Curve,
            Q = new ECPoint()
            {
                X = publicX,
                Y = publicY
            }
        });
        return new()
        {
            DiffieHellman = diffieHellman,
            Nonce = nonce,
            EncryptionParams = encryptionParams
        };
    }

    public byte[] GenerateSharedSecret(CdpEncryptionInfo remoteEncryption)
        => DiffieHellman.DeriveKeyFromHash(remoteEncryption.DiffieHellman.PublicKey, EncryptionParams.KeyDerivationHashAlgorithm, CdpEncryptionParams.SecretPrepend, CdpEncryptionParams.SecretAppend);
}
