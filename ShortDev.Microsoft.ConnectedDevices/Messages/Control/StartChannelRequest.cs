using ShortDev.Networking;
using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

public sealed class StartChannelRequest : ICdpPayload<StartChannelRequest>
{
    public static StartChannelRequest Parse(BinaryReader reader)
        => new()
        {
            Id = reader.ReadStringWithLength(zeroByte: true),
            Name = reader.ReadStringWithLength(zeroByte: true),
            Unknown1 = reader.ReadInt16(),
            Unknown2 = reader.ReadStringWithLength(zeroByte: true)
        };

    public required string Id { get; init; }
    public required string Name { get; init; }
    public required short Unknown1 { get; init; }
    public required string Unknown2 { get; init; }

    public void Write(BinaryWriter writer)
    {
        throw new NotImplementedException();
    }
}
