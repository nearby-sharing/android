#define CheckHmac

using ShortDev.Networking;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;

public sealed class CdpCryptor
{
    byte[] _secret { get; init; }
    public CdpCryptor(byte[] sharedSecret)
        => _secret = sharedSecret;

    byte[] GenerateIV(CommonHeader header)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _secret[16..32];
            using (MemoryStream stream = new())
            using (BigEndianBinaryWriter writer = new(stream)) // Big-endian format!
            {
                writer.Write(header.SessionID);
                writer.Write(header.SequenceNumber);
                writer.Write(header.FragmentIndex);
                writer.Write(header.FragmentCount);

                return aes.EncryptCbc(stream.ToArray(), new byte[16], PaddingMode.None);
            }
        }
    }

    byte[] AesKey
        => _secret[0..16];

    byte[] ComputeHmac(byte[] buffer)
        => new HMACSHA256(_secret[^32..^0]).ComputeHash(buffer);

    static unsafe void AlterMessageLengthUnsafe(byte[] buffer, short delta)
    {
        fixed (byte* pBuffer = buffer)
        {
            Span<byte> msgLengthSpan = new(pBuffer + CommonHeader.MessageLengthOffset, 2);
            BinaryPrimitives.WriteInt16BigEndian(
                msgLengthSpan,
                (short)(BinaryPrimitives.ReadInt16BigEndian(msgLengthSpan) + delta)
            );
        }
    }

    public unsafe byte[] DecryptMessage(CommonHeader header, byte[] payload, byte[]? hmac = null)
    {
        byte[] decryptedPayload;
        using (var aes = Aes.Create())
        {
            byte[] iv = GenerateIV(header);
            aes.Key = AesKey;
            decryptedPayload = aes.DecryptCbc(payload, iv);
        }

        if (header.HasFlag(MessageFlags.HasHMAC))
        {
            if (hmac == null || hmac.Length != Constants.HMacSize)
                throw new InvalidDataException("Invalid hmac!");

            byte[] buffer = ((ICdpWriteable)header).ToArray().Concat(payload).ToArray();
            AlterMessageLengthUnsafe(buffer, -Constants.HMacSize);

            var expectedHMac = ComputeHmac(buffer);
            if (!hmac.SequenceEqual(expectedHMac))
                throw new InvalidDataException("Invalid hmac!");
        }

        return decryptedPayload;
    }

    public unsafe void EncryptMessage(BinaryWriter writer, CommonHeader header, ICdpWriteable[] body)
    {
        EncryptMessage(writer, header, (payloadWriter) =>
        {
            foreach (var item in body)
                item.Write(payloadWriter);
        });
    }

    public unsafe void EncryptMessage(BinaryWriter writer, CommonHeader header, Action<BinaryWriter> bodyCallback)
    {
        using (MemoryStream msgStream = new())
        using (BigEndianBinaryWriter msgWriter = new(msgStream))
        using (var aes = Aes.Create())
        {
            byte[] iv = GenerateIV(header);
            aes.Key = AesKey;

            int payloadLength = sizeof(Int32); // Size of "EncryptedPayloadSize" field
            byte[] payloadBuffer;
            using (MemoryStream bodyStream = new())
            using (BigEndianBinaryWriter bodyWriter = new(bodyStream))
            {
                bodyCallback(bodyWriter);
                payloadBuffer = bodyStream.ToArray();
                payloadLength += payloadBuffer.Length;
            }

            using (MemoryStream payloadStream = new())
            using (BigEndianBinaryWriter payloadWriter = new(payloadStream))
            {
                payloadWriter.Write((uint)payloadLength);

                payloadWriter.Write(payloadBuffer);

                var encryptedPayload = aes.EncryptCbc(payloadStream.ToArray(), iv);

                header.Flags |= MessageFlags.SessionEncrypted | MessageFlags.HasHMAC;
                header.SetMessageLength(encryptedPayload.Length);
                header.Write(msgWriter);

                msgWriter.Write(encryptedPayload);
            }

            byte[] msgBuffer = msgStream.ToArray();
            byte[] hmac = ComputeHmac(msgBuffer);
            AlterMessageLengthUnsafe(msgBuffer, +Constants.HMacSize);

            writer.Write(msgBuffer);
            writer.Write(hmac);
        }
    }

    public BinaryReader Read(BinaryReader reader, CommonHeader header)
    {
        if (!header.HasFlag(MessageFlags.SessionEncrypted))
            return reader;

        int payloadSize = header.PayloadSize;
        if (header.HasFlag(MessageFlags.HasHMAC))
        {
            payloadSize -= Constants.HMacSize;
        }

        byte[] encryptedPayload = reader.ReadBytes(payloadSize);

        byte[]? hmac = null;
        if (header.HasFlag(MessageFlags.HasHMAC))
            hmac = reader.ReadBytes(Constants.HMacSize);

        byte[] decryptedPayload = DecryptMessage(header, encryptedPayload, hmac);
        BigEndianBinaryReader payloadReader = new(new MemoryStream(decryptedPayload));

        var payloadLength = payloadReader.ReadUInt32();
        if (payloadLength != decryptedPayload.Length - sizeof(Int32))
        {
            payloadReader.Dispose();
            throw new InvalidDataException($"Expected payload to be {payloadLength} bytes long");
        }

        return payloadReader;
    }
}