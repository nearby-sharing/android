#define CheckHmac

using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Networking;
using System;
using System.Diagnostics;
using System.Linq;
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

        var aes = _ivAes;

        EndianWriter writer = new(Endianness.BigEndian, Constants.IVSize);

        writer.Write(header.SessionId);
        writer.Write(header.SequenceNumber);
        writer.Write(header.FragmentIndex);
        writer.Write(header.FragmentCount);

        int bytesWritten = aes.EncryptCbc(writer.Buffer.AsSpan(), _ivData, destination, PaddingMode.None);
        Debug.Assert(bytesWritten == destination.Length);
    }

    void ComputeHmac(ReadOnlySpan<byte> buffer, Span<byte> destination)
    {
        Debug.Assert(destination.Length == Constants.HMacSize);

        var isSuccess = _hmac.TryComputeHash(buffer, destination, out var bytesWritten);

        Debug.Assert(isSuccess);
        Debug.Assert(bytesWritten == destination.Length);
    }

    public byte[] DecryptMessage(CommonHeader header, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> hmac)
    {
        Span<byte> iv = stackalloc byte[Constants.IVSize];
        GenerateIV(header, iv);

        byte[] decryptedPayload;
        try
        {
            decryptedPayload = _aes.DecryptCbc(payload, iv);
        }
        catch
        {
            // ToDo: Better way without try...catch!!
            // If payload size is an exact multiple of block length (16 bytes) no padding is applied
            decryptedPayload = _aes.DecryptCbc(payload, iv, PaddingMode.None);
        }

        VerifyHMac(header, payload, hmac);

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

    public void EncryptMessage(EndianWriter writer, CommonHeader header, BodyCallback bodyCallback)
    {
        EndianWriter msgWriter = new(Endianness.BigEndian);

        Span<byte> iv = stackalloc byte[Constants.IVSize];
        GenerateIV(header, iv);

        EndianWriter bodyWriter = new(Endianness.BigEndian);
        bodyCallback(bodyWriter);
        var payloadBuffer = bodyWriter.Buffer.AsSpan();

        EndianWriter payloadWriter = new(Endianness.BigEndian);
        payloadWriter.Write((uint)payloadBuffer.Length);
        payloadWriter.Write(payloadBuffer);

        var buffer = payloadWriter.Buffer.AsSpan();
        // If payload size is an exact multiple of block length (16 bytes) no padding is applied
        PaddingMode paddingMode = buffer.Length % 16 == 0 ? PaddingMode.None : PaddingMode.PKCS7;
        var encryptedPayload = _aes.EncryptCbc(buffer, iv, paddingMode);

        header.Flags |= MessageFlags.SessionEncrypted | MessageFlags.HasHMAC;
        header.SetPayloadLength(encryptedPayload.Length);
        header.Write(msgWriter);

        msgWriter.Write(encryptedPayload);

        var msgBuffer = msgWriter.Buffer.AsWriteableSpan();

        Span<byte> hmac = stackalloc byte[Constants.HMacSize];
        ComputeHmac(msgBuffer, hmac);
        CommonHeader.ModifyMessageLength(msgBuffer, +Constants.HMacSize);

        writer.Write(msgBuffer);
        writer.Write(hmac);
    }

    public EndianReader Read(EndianReader reader, CommonHeader header)
    {
        if (!header.HasFlag(MessageFlags.SessionEncrypted))
            return reader;

        int payloadSize = header.PayloadSize;
        if (header.HasFlag(MessageFlags.HasHMAC))
        {
            payloadSize -= Constants.HMacSize;
        }

        var encryptedPayload = reader.ReadBytes(payloadSize);

        ReadOnlySpan<byte> hmac = ReadOnlySpan<byte>.Empty;
        if (header.HasFlag(MessageFlags.HasHMAC))
            hmac = reader.ReadBytes(Constants.HMacSize);

        byte[] decryptedPayload = DecryptMessage(header, encryptedPayload, hmac);
        EndianReader payloadReader = new(Endianness.BigEndian, decryptedPayload);

        var payloadLength = payloadReader.ReadUInt32();
        if (payloadLength != decryptedPayload.Length - sizeof(Int32))
            throw new CdpSecurityException($"Expected payload to be {payloadLength} bytes long");

        return payloadReader;
    }

    public void Dispose()
    {
        _ivAes.Dispose();
        _aes.Dispose();
        _hmac.Dispose();
    }
}