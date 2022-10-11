using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;
using ShortDev.Networking;
using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;

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

//var provider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(new Guid("F6545836-9428-486A-BAD3-B94B3C0659E3")));
//StreamSocketListener socketListener = new();
//socketListener.ConnectionReceived += SocketListener_ConnectionReceived;
//await socketListener.BindServiceNameAsync(provider.ServiceId.ToString(), SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

//provider.StartAdvertising(socketListener);

//void SocketListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
//{

//}

var adapter = await BluetoothAdapter.GetDefaultAsync();
Debug.Print(adapter.BluetoothAddress.ToString("X"));

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
    if (!CommonHeader.TryParse(reader, out var header, out _) || header == null)
        throw new InvalidDataException();

    foreach (var entry in header.AdditionalHeaders)
    {
        Debug.Print(entry.Type + " " + BinaryConvert.ToString(entry.Value));
    }

    if (header.Type == MessageType.Connect)
    {
        ConnectionHeader connectionHeader = ConnectionHeader.Parse(reader);
        switch (connectionHeader.ConnectMessageType)
        {
            case ConnectionType.ConnectRequest:
                {
                    ConnectionRequest msg = ConnectionRequest.Parse(reader);
                    break;
                }
            case ConnectionType.ConnectResponse:
                {
                    ConnectionResponse msg = ConnectionResponse.Parse(reader);
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