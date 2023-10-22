using ShortDev.Networking;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;

public sealed class DiscoveryHeader : ICdpHeader<DiscoveryHeader>
{
    public static DiscoveryHeader Parse(ref EndianReader reader)
        => new()
        {
            Type = (DiscoveryType)reader.ReadByte()
        };

    public void Write(EndianWriter writer)
    {
        writer.Write((byte)Type);
    }

    public required DiscoveryType Type { get; set; }
}
