using Bond.IO.Unsafe;
using Bond.Protocols;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using ShortDev.Networking;
using Spectre.Console;
using ShortDev.Microsoft.ConnectedDevices.Messages;

//var adapter = await BluetoothAdapter.GetDefaultAsync();
//Debug.Print(adapter.BluetoothAddress.ToString("X"));

var secret = BinaryConvert.ToBytes("37fc508508ba8d6d7ba7ddc79ad29fecdf855879e2a48b6811f310e80dcab98a81500925c1c8019c05b418d3bc22a870fc52d3735b43babc85c57a1fe12d4fb4");
CdpCryptor cryptor = new(secret);

while (true)
{
    Console.Clear();
    var buffer = BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Message"));

    using (MemoryStream stream = new(buffer))
    using (BigEndianBinaryReader reader = new(stream))
    {
        if (!CommonHeader.TryParse(reader, out var header, out _) || header == null)
            throw new InvalidDataException();

        HandleMessage(header, cryptor.Read(reader, header));
    }
    Console.ReadLine();
}

void HandleMessage(CommonHeader header, BinaryReader reader)
{
    if (header.Type == MessageType.Connect)
    {
        ConnectionHeader connectionHeader = ConnectionHeader.Parse(reader);
        switch (connectionHeader.MessageType)
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
            case ConnectionType.DeviceAuthRequest:
            case ConnectionType.DeviceAuthResponse:
                {
                    AuthenticationPayload msg = AuthenticationPayload.Parse(reader);
                    break;
                }
            case ConnectionType.DeviceInfoMessage:
                {
                    var msg = DeviceInfoMessage.Parse(reader);
                    break;
                }
        }
    }
    else if (header.Type == MessageType.Session)
    {
        var fragmentHeader = SessionFragmentHeader.Parse(reader);
        var valueSet = ValueSet.Parse(reader.BaseStream);
    }
    else
    {
        Console.WriteLine();
        Console.WriteLine(BinaryConvert.ToString(reader.ReadPayload()));
    }
}