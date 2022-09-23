using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;
using ShortDev.Networking;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;

//var device = await BluetoothDevice.FromBluetoothAddressAsync(0x00fa213efb18);
//var result = await device.GetRfcommServicesForIdAsync(RfcommServiceId.FromUuid(new(Constants.RfcommServiceId)), BluetoothCacheMode.Uncached);
//var services = result.Services.ToArray();
//var service = services[0];

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

    if (headers.Type == MessageType.Connect)
    {
        ConnectionHeader connectionHeader = new(reader);
        switch (connectionHeader.ConnectMessageType)
        {
            case ConnectionType.ConnectRequest:
                {
                    ConnectionRequest msg = new(reader);
                    break;
                }
            case ConnectionType.ConnectResponse:
                {
                    ConnectionResponse msg = new(reader);
                    break;
                }
            default:
                throw new NotImplementedException();
        }
    }
    else
        throw new NotImplementedException();
    
    // DiscoveryHeaders discoveryHeaders = new(reader);
    // PresenceResponse presenceResponse = new(reader);
    reader.ReadByte();
}