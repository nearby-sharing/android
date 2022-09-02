using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;
using ShortDev.Networking;

Console.Write("Hex: ");
var hex = Console.ReadLine();
if (hex == null)
    throw new ArgumentNullException(nameof(hex));

BinaryConvert.AsBytes(hex, out var length, null);
byte[] buffer = new byte[length];
BinaryConvert.AsBytes(hex, out _, buffer);

using (MemoryStream stream = new(buffer))
using (BigEndianBinaryReader reader = new(stream))
{
    CommonHeaders headers = new();
    headers.TryRead(reader);
    DiscoveryHeaders discoveryHeaders = new(reader);
    PresenceResponse presenceResponse = new(reader);
    reader.ReadByte();

    int c = 0;
    while (true)
    {
        reader.ReadByte();
        c++;
    }
}