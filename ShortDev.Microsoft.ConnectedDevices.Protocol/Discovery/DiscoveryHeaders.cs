using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery
{
    public sealed class DiscoveryHeaders
    {
        public DiscoveryHeaders(BinaryReader reader)
        {
            Type = (DiscoveryType)reader.ReadByte();
        }

        public DiscoveryHeaders Read(BinaryReader reader)
            => this;

        public DiscoveryType Type { get; }
    }
}
