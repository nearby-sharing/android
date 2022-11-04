using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;

/// <summary>
/// The <see cref="ConnectionHeader"/> is common for all Connection Messages.
/// </summary>
public sealed class ConnectionHeader : ICdpHeader<ConnectionHeader>
{
    /// <summary>
    /// Indicates the current connection type.
    /// </summary>
    public required ConnectionType MessageType { get; set; }

    /// <summary>
    /// Displays the types of available connections.
    /// </summary>
    public required ConnectionMode ConnectionMode { get; set; }

    public static ConnectionHeader Parse(BinaryReader reader)
    {
        return new()
        {
            ConnectionMode = (ConnectionMode)reader.ReadInt16(),
            MessageType = (ConnectionType)reader.ReadByte()
        };
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((short)ConnectionMode);
        writer.Write((byte)MessageType);
    }
}
