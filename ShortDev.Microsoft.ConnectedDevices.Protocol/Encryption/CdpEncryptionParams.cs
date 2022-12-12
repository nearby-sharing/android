using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using System;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;

public sealed class CdpEncryptionParams
{
    public static CdpEncryptionParams Default { get; } = new();
    public CdpEncryptionParams FromCurveType(CurveType curveType)
    {
        if (curveType != CurveType.CT_NIST_P256_KDF_SHA512)
            throw new ArgumentException("Invalid curve type", nameof(curveType));
        return new();
    }

    private CdpEncryptionParams() { }

    public ECCurve Curve { get; } = ECCurve.NamedCurves.nistP256;

    public HashAlgorithmName KeyDerivationHashAlgorithm { get; } = HashAlgorithmName.SHA512;

    public byte[] SecretPrepend { get; } = new byte[] { 0x0D6, 0x37, 0x0F1, 0x0AA, 0x0E2, 0x0F0, 0x41, 0x8C };

    public byte[] SecretAppend { get; } = new byte[] { 0x0A8, 0x0F8, 0x1A, 0x57, 0x4E, 0x22, 0x8A, 0x0B7 };
}
