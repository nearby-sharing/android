﻿namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

public readonly record struct ControlHeader : IBinaryWritable, IBinaryParsable<ControlHeader>
{
    public required ControlMessageType MessageType { get; init; }

    public static ControlHeader Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        return new()
        {
            MessageType = (ControlMessageType)reader.ReadUInt8()
        };
    }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write((byte)MessageType);
    }
}
