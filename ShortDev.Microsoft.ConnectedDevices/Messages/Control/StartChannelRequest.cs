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
            Reserved1 = reader.ReadInt16(),
            Reserved2 = reader.ReadStringWithLength(zeroByte: true)
        };

    public required string Id { get; init; }
    public required string Name { get; init; }
    public short Reserved1 { get; init; }
    public string Reserved2 { get; init; } = string.Empty;

    public void Write(BinaryWriter writer)
    {
        writer.WriteWithLength(Id);
        writer.WriteWithLength(Name);
        writer.Write(Reserved1);
        writer.WriteWithLength(Reserved2);
    }
}
