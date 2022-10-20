using ShortDev.Networking;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;

public sealed class CdpEncryptionHelper
{
    byte[] _secret { get; init; }
    private CdpEncryptionHelper() { }

    public static CdpEncryptionHelper FromSecret(byte[] sharedSecret)
    {
        return new()
        {
            _secret = sharedSecret
        };
    }

    public byte[] DecryptMessage(CommonHeader header, byte[] payload)
    {
        byte[] encrypted = payload[0..^32];
        byte[] hmac = payload[^32..^0];

        var aes = Aes.Create();
        aes.Key = _secret[16..32];
        byte[] iv;
        using (MemoryStream ivBuffer = new())
        using (BigEndianBinaryWriter ivWriter = new(ivBuffer))
        {
            ivWriter.Write((long)0x0000000f00000000 + header.RealSessionId); // 0x0000000f00000008 // new byte[] { 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01 }
            ivWriter.Write(header.SequenceNumber);
            ivWriter.Write(header.FragmentIndex);
            ivWriter.Write(header.FragmentCount);
            byte[] ivBufferArr = ivBuffer.ToArray();
            iv = aes.EncryptCbc(ivBufferArr, new byte[16], PaddingMode.None);
        }

        aes.Key = _secret[0..16];
        return aes.DecryptCbc(encrypted, iv, PaddingMode.None);
    }
}
