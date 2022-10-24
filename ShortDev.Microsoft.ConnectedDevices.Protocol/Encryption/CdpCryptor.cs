using ShortDev.Networking;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
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
            using (BigEndianBinaryWriter writer = new(stream))
            {
                writer.Write(header.SessionID);
                writer.Write(header.SequenceNumber);
                writer.Write(header.FragmentIndex);
                writer.Write(header.FragmentCount);

                return aes.EncryptCbc(stream.ToArray(), new byte[16], PaddingMode.None);
            }
        }
    }

    byte[] ComputeHmac(byte[] buffer)
        => new HMACSHA256(_secret[^32..^0]).ComputeHash(buffer);

    public unsafe byte[] DecryptMessage(CommonHeader header, byte[] payload, byte[]? hmac = null)
    {
        byte[] decryptedPayload;
        using (var aes = Aes.Create())
        {
            byte[] iv = GenerateIV(header);
            aes.Key = _secret[0..16];
            decryptedPayload = aes.DecryptCbc(payload, iv);
        }

#if CheckHmac
        if (header.HasFlag(MessageFlags.HasHMAC))
        {
            if (hmac == null || hmac.Length != Constants.HmacSize)
                throw new InvalidDataException("Invalid hmac!");

            byte[] buffer = ((ICdpWriteable)header).ToArray().Concat(payload[0..^Constants.HmacSize]).ToArray();
            fixed (byte* pBuffer = buffer)
            {
                Span<byte> flagSpan = new(pBuffer + CommonHeader.FlagsOffset, 2);
                BinaryPrimitives.WriteInt16BigEndian(
                    flagSpan,
                    (short)(BinaryPrimitives.ReadInt16BigEndian(flagSpan) & ~(short)MessageFlags.HasHMAC)
                );
                BinaryPrimitives.WriteInt16BigEndian(
                    new(pBuffer + CommonHeader.MessageLengthOffset, 2),
                    (short)(((ICdpSerializable<CommonHeader>)header).CalcSize() + decryptedPayload.Length)
                );
            }

            using (MemoryStream stream = new(buffer))
            using (BigEndianBinaryReader reader = new(stream))
            {
                var test = CommonHeader.Parse(reader);
            }

            var testHmac = ComputeHmac(buffer);
            if (!hmac.SequenceEqual(testHmac))
                throw new InvalidDataException("Invalid hmac!");
        }
#endif

        return decryptedPayload;
    }

    public unsafe void EncryptMessage(BinaryWriter writer, CommonHeader header, ICdpWriteable[] body)
    {
        using (MemoryStream stream = new())
        using (BigEndianBinaryWriter bufferWriter = new(stream))
        {
            header.Flags |= MessageFlags.SessionEncrypted;
            header.Write(bufferWriter);
            using (var aes = Aes.Create())
            {
                byte[] iv = GenerateIV(header);
                aes.Key = _secret[0..16];

                byte[] payloadBuffer = new byte[0];
                foreach (var writable in body)
                    payloadBuffer = payloadBuffer.Concat(writable.ToArray()).ToArray();

                bufferWriter.Write((int)payloadBuffer.Length);

                bufferWriter.Write(aes.EncryptCbc(payloadBuffer, iv));
            }
            var buffer = stream.ToArray();
            //var hash = ComputeHmac(buffer);

            //fixed (byte* pBuffer = buffer)
            //{
            //    Span<byte> flagSpan = new(pBuffer + CommonHeader.FlagsOffset, 2);
            //    BinaryPrimitives.WriteInt16BigEndian(
            //        flagSpan,
            //        (short)(BinaryPrimitives.ReadInt16BigEndian(flagSpan) | (short)MessageFlags.HasHMAC)
            //    );
            //}

            writer.Write(buffer);
            //writer.Write(hash);
        }
    }

    public BinaryReader Read(BinaryReader reader, CommonHeader header)
    {
        if (!header.HasFlag(MessageFlags.SessionEncrypted))
            return reader;

        byte[] encryptedPayload = reader.ReadBytes(header.PayloadSize);

        byte[]? hmac = null;
        if (header.HasFlag(MessageFlags.HasHMAC))
            hmac = reader.ReadBytes(Constants.HmacSize);

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