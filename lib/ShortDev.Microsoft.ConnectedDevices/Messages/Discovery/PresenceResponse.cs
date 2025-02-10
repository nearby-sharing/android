﻿using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using System.Security.Cryptography;
using ShortDev.IO;
using System.Diagnostics;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;

public class PresenceResponse : IBinaryWritable, IBinaryParsable<PresenceResponse>
{
    public required ConnectionMode ConnectionMode { get; init; }

    public required DeviceType DeviceType { get; init; }

    public required string DeviceName { get; init; }

    public required int DeviceIdSalt { get; init; }

    public required DeviceIdHash DeviceIdHash { get; init; }

    public static PresenceResponse Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            ConnectionMode = (ConnectionMode)reader.ReadInt16(),
            DeviceType = (DeviceType)reader.ReadInt16(),
            DeviceName = reader.ReadStringWithLength(),
            DeviceIdSalt = reader.ReadInt32(),
            DeviceIdHash = reader.Read<DeviceIdHash>()
        };

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write((short)ConnectionMode);
        writer.Write((short)DeviceType);
        writer.WriteWithLength(DeviceName);
        writer.Write((int)DeviceIdSalt);
        writer.Write<DeviceIdHash>(DeviceIdHash);
    }

    static readonly HashAlgorithm _hashAlgorithm = SHA256.Create();
    public static PresenceResponse Create(LocalDeviceInfo deviceInfo, ConnectionMode connectionMode = ConnectionMode.Proximal)
    {
        var salt = RandomNumberGenerator.GetInt32(int.MaxValue);

        using var writer = EndianWriter.Create(Endianness.LittleEndian, ConnectedDevicesPlatform.MemoryPool);

        // ToDo: Wrong
        writer.Write(salt);
        writer.Write(deviceInfo.GetDeduplicationHint());

        Debug.Assert(_hashAlgorithm.HashSize / 8 == 32);

        DeviceIdHash hash = default;
        _hashAlgorithm.TryComputeHash(writer.Stream.WrittenSpan, hash, out _);

        return new()
        {
            ConnectionMode = connectionMode,
            DeviceName = deviceInfo.Name,
            DeviceType = deviceInfo.Type,
            DeviceIdSalt = salt,
            DeviceIdHash = hash
        };
    }
}
