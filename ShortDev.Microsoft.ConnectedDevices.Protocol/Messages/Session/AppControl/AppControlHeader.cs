using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Messages.Session.AppControl;

public sealed class AppControlHeader : ICdpHeader<AppControlHeader>
{
    public required AppControlType MessageType { get; set; }

    public static AppControlHeader Parse(BinaryReader reader)
    {
        return new()
        {
            MessageType = (AppControlType)reader.ReadByte()
        };
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)MessageType);
    }
}
