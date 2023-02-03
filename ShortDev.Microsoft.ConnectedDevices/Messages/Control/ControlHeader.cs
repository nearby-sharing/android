using ShortDev.Networking;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

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

    public void Write(EndianWriter writer)
    {
        writer.Write((byte)MessageType);
    }
}
