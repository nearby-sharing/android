namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

public sealed class ControlHeader : ICdpHeader<ControlHeader>
{
    public required ControlMessageType MessageType { get; set; }

    public static ControlHeader Parse(ref EndianReader reader)
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
