using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using ShortDev.Networking;

//Thread thread = new(() =>
//{
//    while (true) { }
//});

//BluetoothLEAdvertisementWatcher watcher = new();
//watcher.Received += Watcher_Received;
//watcher.Start();

//async void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
//{
//    
//}

//thread.Start();
//thread.Join();

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
    if (!headers.TryRead(reader))
        throw new InvalidDataException();

    ConnectionHeader connectionHeader = new(reader);
    reader.ReadByte();
    ConnectionRequest connectionRequest = new(reader);
    // DiscoveryHeaders discoveryHeaders = new(reader);
    // PresenceResponse presenceResponse = new(reader);
    reader.ReadByte();
}