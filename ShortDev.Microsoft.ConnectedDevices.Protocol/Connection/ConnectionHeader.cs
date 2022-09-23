using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;

public sealed class ConnectionHeader : ICdpHeader<ConnectionHeader>
{
    public required ConnectionType ConnectMessageType { get; set; }

    public required ConnectionMode ConnectionMode { get; set; }

    public static ConnectionHeader Parse(BinaryReader reader)
    {
        return new()
        {
            ConnectionMode = (ConnectionMode)reader.ReadInt16(),
            ConnectMessageType = (ConnectionType)reader.ReadByte()
        };
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((short)ConnectionMode);
        writer.Write((byte)ConnectMessageType);
    }
}
