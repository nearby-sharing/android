using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;

public sealed class DiscoveryHeader : ICdpHeader<DiscoveryHeader>
{
    public static DiscoveryHeader Parse(BinaryReader reader)
        => new()
        {
            Type = (DiscoveryType)reader.ReadByte()
        };

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Type);
    }

    public required DiscoveryType Type { get; set; }
}
