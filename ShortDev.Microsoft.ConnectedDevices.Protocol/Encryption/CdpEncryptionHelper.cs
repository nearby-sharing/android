using ShortDev.Networking;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;

public sealed class CdpEncryptionHelper
{
    byte[] _secret { get; init; }
    public CdpEncryptionHelper(byte[] sharedSecret)
        => _secret = sharedSecret;

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
            ivWriter.Write(header.SessionID);
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
