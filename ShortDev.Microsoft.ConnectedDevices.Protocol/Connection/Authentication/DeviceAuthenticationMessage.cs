using ShortDev.Networking;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.Authentication;

public sealed class DeviceAuthenticationMessage : ICdpPayload<DeviceAuthenticationMessage>
{
    public static DeviceAuthenticationMessage Parse(BinaryReader reader)
        => new()
        {
            DeviceCert = reader.ReadBytesWithLength(),
            SignedThumbprint = reader.ReadBytesWithLength()
        };

    public required byte[] DeviceCert { get; init; }
    public required byte[] SignedThumbprint { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteWithLength(DeviceCert);
        writer.WriteWithLength(SignedThumbprint);
    }
}
