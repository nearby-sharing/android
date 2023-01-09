using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Messages.Control;

public sealed class StartChannelResponse : ICdpPayload<StartChannelResponse>
{
    public static StartChannelResponse Parse(BinaryReader reader)
    {
        throw new NotImplementedException();
    }

    public required long ReponseId { get; init; }
    public required int Unknown { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(ReponseId);
        writer.Write(Unknown);
        writer.Write((uint)0);
    }
}
