using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection;

/// <summary>
/// The type of elliptical curve used. <br/>
/// (See <see cref="ConnectionRequest.CurveType"/>) <br/>
/// (See <see cref="Encryption.CdpEncryptionParams.FromCurveType(CurveType)"/>
/// </summary>
public enum CurveType : byte
{
    /// <summary>
    /// Indicates <see cref="ECCurve.NamedCurves.nistP256"/> with <see cref="HashAlgorithmName.SHA512"/>
    /// </summary>
    CT_NIST_P256_KDF_SHA512
}
