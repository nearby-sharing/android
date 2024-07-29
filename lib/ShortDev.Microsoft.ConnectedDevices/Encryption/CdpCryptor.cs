using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Encryption;

public sealed class CdpCryptor : IDisposable
{
    static readonly byte[] _ivData = new byte[16];

    readonly Aes _ivAes;
    readonly Aes _aes;
    readonly HMACSHA256 _hmac;
    public CdpCryptor(byte[] sharedSecret)
    {
        _aes = Aes.Create();
        _aes.Key = sharedSecret[..16];

        _ivAes = Aes.Create();
        _ivAes.Key = sharedSecret[16..32];

        _hmac = new(sharedSecret[^32..^0]);
    }

    void GenerateIV(CommonHeader header, Span<byte> destination)
    {
        Debug.Assert(destination.Length == Constants.IVSize);

        Span<byte> raw = stackalloc byte[Constants.IVSize];
        BinaryPrimitives.WriteUInt64BigEndian(raw[..8], header.SessionId);
        BinaryPrimitives.WriteUInt32BigEndian(raw[8..12], header.SequenceNumber);
        BinaryPrimitives.WriteUInt16BigEndian(raw[12..14], header.FragmentIndex);
        BinaryPrimitives.WriteUInt16BigEndian(raw[14..16], header.FragmentCount);

        int bytesWritten = _ivAes.EncryptCbc(raw, _ivData, destination, PaddingMode.None);
        Debug.Assert(bytesWritten == destination.Length);
    }

    void ComputeHmac(ReadOnlySpan<byte> buffer, Span<byte> destination)
    {
        Debug.Assert(destination.Length == Constants.HMacSize);

        var isSuccess = _hmac.TryComputeHash(buffer, destination, out var bytesWritten);

        Debug.Assert(isSuccess);
        Debug.Assert(bytesWritten == destination.Length);
    }

    public ReadOnlyMemory<byte> DecryptMessage(CommonHeader header, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> hmac)
    {
        VerifyHMac(header, payload, hmac);

        Span<byte> iv = stackalloc byte[Constants.IVSize];
        GenerateIV(header, iv);

        byte[] decryptedPayload = _aes.DecryptCbc(payload, iv, PaddingMode.None);

        if (HasPadding(decryptedPayload, out var paddingSize))
            return decryptedPayload.AsMemory()[0..^paddingSize];

        return decryptedPayload;
    }

    void VerifyHMac(CommonHeader header, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> hmac)
    {
        if (!header.HasFlag(MessageFlags.HasHMAC))
            return;

        if (hmac == null || hmac.Length != Constants.HMacSize)
            throw new CdpSecurityException("Invalid hmac size!");

        EndianWriter writer = new(Endianness.BigEndian);
        header.Write(writer);
        writer.Write(payload);

        var buffer = writer.Buffer.AsWriteableSpan();
        CommonHeader.ModifyMessageLength(buffer, -Constants.HMacSize);

        Span<byte> expectedHMac = stackalloc byte[Constants.HMacSize];
        ComputeHmac(buffer, expectedHMac);
        if (!hmac.SequenceEqual(expectedHMac))
            throw new CdpSecurityException("Invalid hmac!");
    }

    public void EncryptMessage(IFragmentSender sender, CommonHeader header, ReadOnlySpan<byte> payloadBuffer)
    {
        // Prepend payload with length
        ReadOnlySpan<byte> finalPayload;
        {
            EndianWriter payloadWriter = new(Endianness.BigEndian);
            payloadWriter.Write((uint)payloadBuffer.Length);
            payloadWriter.Write(payloadBuffer);

            finalPayload = payloadWriter.Buffer.AsSpan();
        }

        // Encrypt
        var msgWriter = Encrypt(header, finalPayload);

        // HMAC
        {
            var msgBuffer = msgWriter.Buffer.AsWriteableSpan();
            Span<byte> hmac = stackalloc byte[Constants.HMacSize];
            ComputeHmac(msgBuffer, hmac);
            CommonHeader.ModifyMessageLength(msgBuffer, +Constants.HMacSize);
            msgWriter.Write(hmac);
        }

        sender.SendFragment(msgWriter.Buffer.AsSpan());
    }

    EndianWriter Encrypt(CommonHeader header, ReadOnlySpan<byte> buffer)
    {
        // If payload size is an exact multiple of block length (16 bytes) no padding is applied
        PaddingMode paddingMode = buffer.Length % 16 == 0 ? PaddingMode.None : PaddingMode.PKCS7;
        var encryptedPayloadLength = _aes.GetCiphertextLengthCbc(buffer.Length, paddingMode);

        // Write header
        EndianWriter writer = new(Endianness.BigEndian);
        header.Flags |= MessageFlags.SessionEncrypted | MessageFlags.HasHMAC;
        header.SetPayloadLength(encryptedPayloadLength);
        header.Write(writer);

        Span<byte> iv = stackalloc byte[Constants.IVSize];
        GenerateIV(header, iv);

        IBufferWriter<byte> bufferWriter = writer.Buffer;

        // Encrypt and write to msgWriter
        _aes.EncryptCbc(buffer, iv, bufferWriter.GetSpan(encryptedPayloadLength), paddingMode);
        bufferWriter.Advance(encryptedPayloadLength);

        return writer;
    }

    public void Read(ref EndianReader reader, CommonHeader header)
    {
        if (!header.HasFlag(MessageFlags.SessionEncrypted))
            return;

        int payloadSize = header.PayloadSize;
        if (header.HasFlag(MessageFlags.HasHMAC))
        {
            payloadSize -= Constants.HMacSize;
        }

        var encryptedPayload = reader.ReadBytes(payloadSize);

        scoped Span<byte> hmac = [];
        if (header.HasFlag(MessageFlags.HasHMAC))
        {
            hmac = stackalloc byte[Constants.HMacSize];
            reader.ReadBytes(hmac);
        }

        var decryptedPayload = DecryptMessage(header, encryptedPayload, hmac);
        reader = new(Endianness.BigEndian, decryptedPayload.Span);

        var payloadLength = reader.ReadUInt32();
        if (payloadLength != decryptedPayload.Length - sizeof(Int32))
            throw new CdpSecurityException($"Expected payload to be {payloadLength} bytes long");
    }

    public void Dispose()
    {
        _ivAes.Dispose();
        _aes.Dispose();
        _hmac.Dispose();
    }

    static bool HasPadding(ReadOnlySpan<byte> buffer, out byte paddingSize)
    {
        paddingSize = buffer[^1];
        for (int i = buffer.Length - paddingSize; i < buffer.Length; i++)
        {
            if (paddingSize != buffer[i])
                return false;
        }
        return true;
    }
}