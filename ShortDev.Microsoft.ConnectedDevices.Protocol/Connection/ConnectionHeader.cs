using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;

public sealed class ConnectionHeader
{
    public ConnectionHeader(BinaryReader reader)
    {
        ConnectionMode = (ConnectionMode)reader.ReadInt16();
        ConnectMessageType = (ConnectionType)reader.ReadByte();
    }

    public ConnectionType ConnectMessageType { get; }

    public ConnectionMode ConnectionMode { get; }
}
