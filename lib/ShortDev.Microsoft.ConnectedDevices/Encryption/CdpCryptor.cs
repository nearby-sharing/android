using ShortDev.IO.Buffers;
using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Encryption;

public sealed class CdpCryptor : IDisposable
{
    static readonly byte[] _ivData = new byte[16];

    readonly Aes _ivAes;
    readonly Aes _aes;
    readonly ReadOnlyMemory<byte> _hmac;
    public CdpCryptor(byte[] sharedSecret)
    {
        _aes = Aes.Create();
        _aes.Key = sharedSecret[..16];

        _ivAes = Aes.Create();
        _ivAes.Key = sharedSecret[16..32];

        _hmac = sharedSecret.AsMemory()[^32..^0];
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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

        var isSuccess = HMACSHA256.TryHashData(_hmac.Span, buffer, destination, out var bytesWritten);

        Debug.Assert(isSuccess);
        Debug.Assert(bytesWritten == destination.Length);
    }

    public PooledMemory<byte> DecryptMessage(CommonHeader header, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> hmac)
    {
        VerifyHMac(header, payload, hmac);

        Span<byte> iv = stackalloc byte[Constants.IVSize];
        GenerateIV(header, iv);

        var decryptedBufferSize = _aes.GetCiphertextLengthCbc(payload.Length, PaddingMode.None);

        var decryptedPayload = ConnectedDevicesPlatform.MemoryPool.RentMemory(decryptedBufferSize);
        _aes.DecryptCbc(payload, iv, decryptedPayload.Span, PaddingMode.None);

        var payloadSize = BinaryPrimitives.ReadUInt32BigEndian(decryptedPayload.AsSpan()[..sizeof(uint)]);
        return decryptedPayload.AsMemory(sizeof(uint), (int)payloadSize);
    }

    void VerifyHMac(CommonHeader header, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> hmac)
    {
        if (!header.HasFlag(MessageFlags.HasHMAC))
            return;

        if (hmac.Length != Constants.HMacSize)
            throw new CdpSecurityException("Invalid hmac size!");

        using var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
        header.Write(writer);
        writer.Write(payload);

        var buffer = writer.Buffer.WrittenSpan;
        CommonHeader.ModifyMessageLength(buffer.AsSpanUnsafe(), -Constants.HMacSize);

        Span<byte> expectedHMac = stackalloc byte[Constants.HMacSize];
        ComputeHmac(buffer, expectedHMac);
        if (!hmac.SequenceEqual(expectedHMac))
            throw new CdpSecurityException("Invalid hmac!");
    }

    public void EncryptMessage(IFragmentSender sender, CommonHeader header, ReadOnlySpan<byte> payloadBuffer)
    {
        using var msgWriter = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);

        using (EndianWriter payloadWriter = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool, payloadBuffer.Length + sizeof(uint)))
        {
            // Prepend payload with length
            payloadWriter.Write((uint)payloadBuffer.Length);
            payloadWriter.Write(payloadBuffer);

            // Encrypt
            Encrypt(msgWriter, header, payloadWriter.Buffer.WrittenSpan);
        }

        // HMAC
        {
            var msgBuffer = msgWriter.Buffer.WrittenSpan;
            Span<byte> hmac = stackalloc byte[Constants.HMacSize];
            ComputeHmac(msgBuffer, hmac);
            CommonHeader.ModifyMessageLength(msgBuffer.AsSpanUnsafe(), +Constants.HMacSize);
            msgWriter.Write(hmac);
        }

        sender.SendFragment(msgWriter.Buffer.WrittenSpan);
    }

    [SkipLocalsInit]
    void Encrypt(EndianWriter writer, CommonHeader header, ReadOnlySpan<byte> buffer)
    {
        // If payload size is an exact multiple of block length (16 bytes) no padding is applied
        PaddingMode paddingMode = buffer.Length % 16 == 0 ? PaddingMode.None : PaddingMode.PKCS7;
        var encryptedPayloadLength = _aes.GetCiphertextLengthCbc(buffer.Length, paddingMode);

        // Write header
        header.Flags |= MessageFlags.SessionEncrypted | MessageFlags.HasHMAC;
        header.SetPayloadLength(encryptedPayloadLength);
        header.Write(writer);

        Span<byte> iv = stackalloc byte[Constants.IVSize];
        GenerateIV(header, iv);

        // Encrypt and write to msgWriter
        _aes.EncryptCbc(buffer, iv, writer.Buffer.GetSpan(encryptedPayloadLength), paddingMode);
        writer.Buffer.Advance(encryptedPayloadLength);
    }

    public DisposeToken<byte> Read(ref EndianReader reader, CommonHeader header)
    {
        if (!header.HasFlag(MessageFlags.SessionEncrypted))
            return default;

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
        reader = EndianReader.Create(Endianness.BigEndian, decryptedPayload.Span);
        return decryptedPayload;
    }

    public void Dispose()
    {
        _ivAes.Dispose();
        _aes.Dispose();
    }
}