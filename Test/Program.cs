using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Platforms.Network;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Networking;
using Spectre.Console;
using System.Diagnostics;
using System.Text.Json;

var secret = BinaryConvert.ToBytes("c77e603b1e78507906bc34e113f59f7242405a57d8af49ad1c65c5b6c8967f81fc1a0bd9742c650b585a26f6876460a476f4c412cb606f96183203c088a733d0");
CdpCryptor cryptor = new(secret);

while (true)
{
    Console.Clear();
    var buffer = BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Message"));

    EndianReader reader = new(Endianness.BigEndian, buffer);

    if (!CommonHeader.TryParse(ref reader, out var header, out _) || header == null)
        throw new InvalidDataException();

    cryptor.Read(ref reader, header);
    HandleMessage(header, reader);

    Console.ReadLine();
}

#pragma warning disable IDE0059 // Unnecessary assignment of a value
static void HandleMessage(CommonHeader header, EndianReader reader)
{
    if (header.Type == MessageType.Connect)
    {
        ConnectionHeader connectionHeader = ConnectionHeader.Parse(ref reader);
        switch (connectionHeader.MessageType)
        {
            case ConnectionType.ConnectRequest:
                {
                    ConnectionRequest msg = ConnectionRequest.Parse(ref reader);
                    break;
                }
            case ConnectionType.ConnectResponse:
                {
                    ConnectionResponse msg = ConnectionResponse.Parse(ref reader);
                    break;
                }
            case ConnectionType.DeviceAuthRequest:
            case ConnectionType.DeviceAuthResponse:
                {
                    AuthenticationPayload msg = AuthenticationPayload.Parse(ref reader);
                    break;
                }
            case ConnectionType.DeviceInfoMessage:
                {
                    var msg = DeviceInfoMessage.Parse(ref reader);
                    break;
                }
        }
    }
    else if (header.Type == MessageType.Session)
    {
        BinaryMsgHeader binaryHeader = BinaryMsgHeader.Parse(ref reader);
        var valueSet = ValueSet.Parse(ref reader);
        Debug.Print(JsonSerializer.Serialize(valueSet));
    }
    else if (header.Type == MessageType.Control)
    {
        ControlHeader controlHeader = ControlHeader.Parse(ref reader);
        switch (controlHeader.MessageType)
        {
            case ControlMessageType.StartChannelRequest:
                StartChannelRequest startChannelRequest = StartChannelRequest.Parse(ref reader);
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