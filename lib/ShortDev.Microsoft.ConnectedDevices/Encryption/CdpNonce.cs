using System;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Encryption;

public readonly record struct CdpNonce(ulong Value)
{
    public override string ToString()
        => Value.ToString();

    public static unsafe CdpNonce Create()
    {
        using RandomNumberGenerator cryptographicRandom = RandomNumberGenerator.Create();
        Span<byte> buffer = stackalloc byte[sizeof(Int64)];
        cryptographicRandom.GetBytes(buffer);
        fixed (byte* pBuffer = buffer)
            return new(*(ulong*)pBuffer);
    }
}
