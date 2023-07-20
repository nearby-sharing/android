using ShortDev.Networking;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Session.AppControl;

public sealed class AppControlHeader : ICdpHeader<AppControlHeader>
{
    public required AppControlType MessageType { get; set; }

    public static AppControlHeader Parse(ref EndianReader reader)
    {
        return new()
        {
            MessageType = (AppControlType)reader.ReadByte()
        };
    }

    public void Write(EndianWriter writer)
    {
        writer.Write((byte)MessageType);
    }
}
