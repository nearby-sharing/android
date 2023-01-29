using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using System.IO;
using ShortDev.Networking;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;

public class PresenceResponse : ICdpPayload<PresenceResponse>
{
    public required ConnectionMode ConnectionMode { get; init; }

    public required DeviceType DeviceType { get; init; }

    public required string DeviceName { get; init; }

    public int DeviceIdSalt { get; init; }

    public int DeviceIdHash { get; init; }

    public int PrincipalUserNameHash { get; init; }

    public static PresenceResponse Parse(EndianReader reader)
        => new()
        {
            ConnectionMode = (ConnectionMode)reader.ReadInt16(),
            DeviceType = (DeviceType)reader.ReadInt16(),
            DeviceName = reader.ReadStringWithLength(),
            DeviceIdSalt = reader.ReadInt32(),
            DeviceIdHash = reader.ReadInt32(),
            PrincipalUserNameHash = reader.ReadInt32()
        };

    public void Write(EndianWriter writer)
    {
        writer.Write((short)ConnectionMode);
        writer.Write((short)DeviceType);
        writer.WriteWithLength(DeviceName);
        writer.Write((int)DeviceIdSalt);
        writer.Write((int)DeviceIdHash);
        writer.Write((int)PrincipalUserNameHash);
    }
}
