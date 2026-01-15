using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Encryption;

public sealed class CdpEncryptionInfo
{
    public required CdpNonce Nonce { get; init; }
    public required ECDiffieHellman DiffieHellman { get; init; }
    public required CdpEncryptionParams EncryptionParams { get; init; }

    public ECPoint PublicKey
        => DiffieHellman.ExportParameters(false).Q;

    public static CdpEncryptionInfo Create(CdpEncryptionParams encryptionParams)
        => new()
        {
            DiffieHellman = ECDiffieHellman.Create(encryptionParams.Curve),
            Nonce = CdpNonce.Create(),
            EncryptionParams = encryptionParams
        };

    public static CdpEncryptionInfo FromRemote(ReadOnlyMemory<byte> publicX, ReadOnlyMemory<byte> publicY, CdpNonce nonce, CdpEncryptionParams encryptionParams)
    {
        var diffieHellman = ECDiffieHellman.Create(new ECParameters()
        {
            Curve = encryptionParams.Curve,
            Q = new ECPoint()
            {
                X = publicX.ToArray(),
                Y = publicY.ToArray()
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
