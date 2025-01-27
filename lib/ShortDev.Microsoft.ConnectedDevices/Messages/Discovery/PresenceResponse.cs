using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;

public class PresenceResponse : ICdpPayload<PresenceResponse>
{
    public required ConnectionMode ConnectionMode { get; init; }

    public required DeviceType DeviceType { get; init; }

    public required string DeviceName { get; init; }

    public required int DeviceIdSalt { get; init; }

    public required byte[] DeviceIdHash { get; init; }

    public static PresenceResponse Parse(ref EndianReader reader)
        => new()
        {
            ConnectionMode = (ConnectionMode)reader.ReadInt16(),
            DeviceType = (DeviceType)reader.ReadInt16(),
            DeviceName = reader.ReadStringWithLength(),
            DeviceIdSalt = reader.ReadInt32(),
            DeviceIdHash = reader.ReadBytes(32).ToArray()
        };

    public void Write(EndianWriter writer)
    {
        writer.Write((short)ConnectionMode);
        writer.Write((short)DeviceType);
        writer.WriteWithLength(DeviceName);
        writer.Write((int)DeviceIdSalt);
        writer.Write(DeviceIdHash);
    }

    static readonly HashAlgorithm _hashAlgorithm = SHA256.Create();
    public static PresenceResponse Create(LocalDeviceInfo deviceInfo, ConnectionMode connectionMode = ConnectionMode.Proximal)
    {
        var salt = RandomNumberGenerator.GetInt32(int.MaxValue);

        using var writer = EndianWriter.Create(Endianness.LittleEndian, ConnectedDevicesPlatform.MemoryPool);

        // ToDo: Wrong
        writer.Write(salt);
        writer.Write(deviceInfo.GetDeduplicationHint());

        var hash = new byte[_hashAlgorithm.HashSize];
        _hashAlgorithm.TryComputeHash(writer.Buffer.WrittenSpan, hash, out _);

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
