using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using ShortDev.Networking;
using Spectre.Console;
using System.IO.Pipes;

//var adapter = await BluetoothAdapter.GetDefaultAsync();
//Debug.Print(adapter.BluetoothAddress.ToString("X"));

{
    NamedPipeServerStream pipeServer = new("\\\\.\\pipe\\CDPInOut", PipeDirection.InOut);
    Console.WriteLine("Waiting...");
    pipeServer.WaitForConnection();
    Console.WriteLine("Connected!!");
    using StreamReader reader = new(pipeServer);
    while (true)
        Console.Write(reader.ReadToEnd());
}

var secret = BinaryConvert.ToBytes("37fc508508ba8d6d7ba7ddc79ad29fecdf855879e2a48b6811f310e80dcab98a81500925c1c8019c05b418d3bc22a870fc52d3735b43babc85c57a1fe12d4fb4");
CdpCryptor cryptor = new(secret);

while (true)
{
    Console.Clear();
    var buffer = BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Message"));

    EndianReader reader = new(Endianness.BigEndian, buffer);

    if (!CommonHeader.TryParse(reader, out var header, out _) || header == null)
        throw new InvalidDataException();

    HandleMessage(header, cryptor.Read(reader, header));

    Console.ReadLine();
}

void HandleMessage(CommonHeader header, EndianReader reader)
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
        BinaryMsgHeader binaryHeader = BinaryMsgHeader.Parse(reader);
        var valueSet = ValueSet.Parse(reader);
    }
    else if (header.Type == MessageType.Control)
    {
        ControlHeader controlHeader = ControlHeader.Parse(reader);
        switch (controlHeader.MessageType)
        {
            case ControlMessageType.StartChannelRequest:
                StartChannelRequest startChannelRequest = StartChannelRequest.Parse(reader);
                break;
            case ControlMessageType.StartChannelResponse:
                break;
            case ControlMessageType.EnumerateAppsReponse:
                break;
            case ControlMessageType.EnumerateAppTargetNamesRequest:
                break;
            case ControlMessageType.EnumerateAppTargetNamesResponse:
                break;
            default:
                break;
        }
    }
    else
    {
        Console.WriteLine();
        //Console.WriteLine(BinaryConvert.ToString(reader.ReadPayload()));
    }
}