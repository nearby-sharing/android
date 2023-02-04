using ShortDev.Networking;
using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

public sealed class StartChannelResponse : ICdpPayload<StartChannelResponse>
{
    public static StartChannelResponse Parse(EndianReader reader)
    {
        throw new NotImplementedException();
    }

    public required long ReponseId { get; init; }
    public required int Unknown { get; init; }

    public void Write(EndianWriter writer)
    {
        writer.Write(ReponseId);
        writer.Write(Unknown);
        writer.Write((uint)0);
    }
}
