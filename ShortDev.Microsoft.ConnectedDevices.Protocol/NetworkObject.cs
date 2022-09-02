using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol
{
    public abstract class NetworkObject
    {
        public NetworkObject() { }

        public NetworkObject(BinaryReader reader) { }

        public void Write(BinaryWriter writer) { }
    }
}
