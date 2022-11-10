using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;
using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Protocol.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Serialization;
using ShortDev.Networking;
using Spectre.Console;
using System.IO;
using System.Reflection.PortableExecutable;

//var adapter = await BluetoothAdapter.GetDefaultAsync();
//Debug.Print(adapter.BluetoothAddress.ToString("X"));


// var valueSet = ValueSet.Parse(File.OpenRead(@"D:\test.bin")); // BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Bytes")));
{
    CompactBinaryReader<InputStream> reader = new(new(File.OpenRead(@"D:\test2.bin")));
    reader.ReadFieldBegin(out var a, out var b);
    // var xyz = Deserialize<PropertyValue>.From(reader);
}
Console.ReadLine();

while (true)
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

var secret = BinaryConvert.ToBytes("941b1425b0d03cc96206b8b76a3b9e1486b703a3068022753bbaf72894a1a126189a78b0252cc38097621c81fe338b5dd8734ec74f862175eacf10f0086e8d09"); //  37fc508508ba8d6d7ba7ddc79ad29fecdf855879e2a48b6811f310e80dcab98a81500925c1c8019c05b418d3bc22a870fc52d3735b43babc85c57a1fe12d4fb4"); // AnsiConsole.Ask<string>("Secret"));
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