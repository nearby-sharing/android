using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

/// <summary>
/// General payload to represent a HRESULT (<see cref="int"/>).
/// </summary>
public sealed class HResultPayload : ICdpPayload<HResultPayload>
{
    public static HResultPayload Parse(BinaryReader reader)
        => new()
        {
            HResult = reader.ReadInt32()
        };

    public required int HResult { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(HResult);
    }
}