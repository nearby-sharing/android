using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection
{
    public sealed class ConnectionHeader
    {
        public ConnectionHeader(BinaryReader reader)
        {
            ConnectMessageType = (ConnectType)reader.ReadByte();
            ConnectionMode = (ConnectionMode)reader.ReadByte();
        }

        public ConnectType ConnectMessageType { get; }

        public ConnectionMode ConnectionMode { get; }
    }
}
