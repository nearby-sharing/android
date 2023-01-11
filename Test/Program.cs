using Bond.IO.Unsafe;
using Bond.Protocols;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using ShortDev.Networking;
using Spectre.Console;
using ShortDev.Microsoft.ConnectedDevices.Messages;

//var adapter = await BluetoothAdapter.GetDefaultAsync();
//Debug.Print(adapter.BluetoothAddress.ToString("X"));

while (false)
{
    MemoryStream stream = new(BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Bytes")));
    CompactBinaryReader<InputStream> reader = new(new(stream));
    reader.ReadFieldBegin(out var a, out var b);
    var typeId = reader.ReadInt32();
    reader.ReadFieldBegin(out var fieldType, out var fieldId);
    bool array = fieldType == Bond.BondDataType.BT_LIST;
    int length = 0;
    if (array)
        reader.ReadContainerBegin(out length, out fieldType);
    Console.WriteLine($"{fieldType} = {typeId}; {fieldId}: {(array ? $"array Count = {length}" : "")}");

    Console.ReadLine();
}

var secret = BinaryConvert.ToBytes("3036f355c9b027833f5ab36fdc0bf313bb6382394a01a40667c6e5f974ad180ab99d444f8b30cff7665c0a7c4ebd870488fdb8f44c9dd5800a060b67484c2116");
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
        // reader.PrintPayload();
        var prepend = reader.ReadBytes(0x0000000C);
        var valueSet = ValueSet.Parse(reader.BaseStream);
    }
    else
    {
        Console.WriteLine();
        Console.WriteLine(BinaryConvert.ToString(reader.ReadPayload()));
    }
}