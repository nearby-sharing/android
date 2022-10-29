using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Control;

public sealed class ControlHeader : ICdpHeader<ControlHeader>
{
    public required ControlMessageType MessageType { get; set; }

    public static ControlHeader Parse(BinaryReader reader)
    {
        return new()
        {
            MessageType = (ControlMessageType)reader.ReadByte()
        };
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)MessageType);
    }
}
