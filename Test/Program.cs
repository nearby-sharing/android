using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Networking;
using Spectre.Console;

//var adapter = await BluetoothAdapter.GetDefaultAsync();
//Debug.Print(adapter.BluetoothAddress.ToString("X"));

var secret = BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Secret"));
var buffer = BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Message"));

CdpCryptor cryptor = new(secret);

bool flag = true;
loop:
using (MemoryStream stream = new(buffer))
using (BigEndianBinaryReader reader = new(stream))
{
    if (!CommonHeader.TryParse(reader, out var header, out _) || header == null)
        throw new InvalidDataException();

    if (flag)
        HandleMessage(header, cryptor.Read(reader, header));
    else
    {
        using (MemoryStream stream2 = new())
        using (BigEndianBinaryWriter writer = new(stream))
        {
        }
        flag = true;
        goto loop;
    }
}

void HandleMessage(CommonHeader header, BinaryReader reader)
{
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
            case ConnectionType.DeviceAuthRequest:
                {
                    AuthenticationPayload msg = AuthenticationPayload.Parse(reader);
                    break;
                }
            default:
                throw new NotImplementedException();
        }
    }
    else
        throw new NotImplementedException();
}