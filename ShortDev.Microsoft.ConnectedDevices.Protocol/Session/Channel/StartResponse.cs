using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Session.Channel;

public sealed class StartResponse : ICdpPayload<StartResponse>
{
    public static StartResponse Parse(BinaryReader reader)
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
