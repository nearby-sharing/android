using System.IO;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;

public readonly struct CdpNonce
{
    public readonly byte[] Value;

    public CdpNonce(byte[] value)
    {
        if (value.Length != Constants.NonceLength)
            throw new InvalidDataException("Invalid nonce length");

        Value = value;
    }

    public static CdpNonce Create()
    {
        using (RandomNumberGenerator cryptographicRandom = RandomNumberGenerator.Create())
        {
            byte[] buffer = new byte[Constants.NonceLength];
            cryptographicRandom.GetBytes(buffer);
            return new(buffer);
        }
    }
}
