using ShortDev.Networking;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.Authentication;

public sealed class DeviceAuthenticationMessage : ICdpPayload<DeviceAuthenticationMessage>
{
    public static DeviceAuthenticationMessage Parse(BinaryReader reader)
        => new()
        {
            DeviceCert = new(reader.ReadBytesWithLength()),
            SignedThumbprint = reader.ReadBytesWithLength()
        };

    public required X509Certificate2 DeviceCert { get; init; }
    public required byte[] SignedThumbprint { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteWithLength(DeviceCert.Export(X509ContentType.Cert));
        writer.WriteWithLength(SignedThumbprint);
    }
}
