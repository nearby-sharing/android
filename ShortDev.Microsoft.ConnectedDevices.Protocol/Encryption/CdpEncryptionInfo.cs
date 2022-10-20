using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;

public sealed class CdpEncryptionInfo
{
    public static readonly ECCurve CurveType = ECCurve.NamedCurves.nistP256;

    public required CdpNonce Nonce { get; init; }
    public required ECDiffieHellman DiffieHellman { get; init; }

    public ECPoint PublicKey
        => DiffieHellman.ExportParameters(false).Q;

    public static CdpEncryptionInfo Create()
        => new()
        {
            DiffieHellman = ECDiffieHellman.Create(CurveType),
            Nonce = CdpNonce.Create()
        };

    public static CdpEncryptionInfo FromRemote(byte[] publicX, byte[] publicY, CdpNonce nonce)
    {
        var diffieHellman = ECDiffieHellman.Create(new ECParameters()
        {
            Curve = CurveType,
            Q = new ECPoint()
            {
                X = publicX,
                Y = publicY
            }
        });
        return new()
        {
            DiffieHellman = diffieHellman,
            Nonce = nonce
        };
    }

    public byte[] GenerateSharedSecret(CdpEncryptionInfo remoteEncryption)
    {
        HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA512;
        // var extractedSecret = DiffieHellman.DeriveKeyFromHash(remoteEncryption.DiffieHellman.PublicKey, hashAlgorithm);
        var extractedSecret = DiffieHellman.DeriveKeyFromHmac(remoteEncryption.DiffieHellman.PublicKey, hashAlgorithm, null);
        return extractedSecret; //  HKDF.Expand(hashAlgorithm, extractedSecret, 64);
    }
}
