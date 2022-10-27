using System;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;

public readonly struct CdpNonce
{
    public readonly ulong Value;

    public CdpNonce(ulong value)
        => Value = value;

    public static unsafe CdpNonce Create()
    {
        using (RandomNumberGenerator cryptographicRandom = RandomNumberGenerator.Create())
        {
            Span<byte> buffer = stackalloc byte[sizeof(Int64)];
            cryptographicRandom.GetBytes(buffer);
            fixed (byte* pBuffer = buffer)
                return new(*(ulong*)pBuffer);
        }
    }
}
