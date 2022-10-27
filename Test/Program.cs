using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Networking;
using Spectre.Console;

//var adapter = await BluetoothAdapter.GetDefaultAsync();
//Debug.Print(adapter.BluetoothAddress.ToString("X"));

var secret = BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Secret"));
var remoteNonce = AnsiConsole.Ask<ulong>("Remote Nonce");
var buffer = BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Message"));

var localEncryption = CdpEncryptionInfo.Create(CdpEncryptionParams.Default);
CdpCryptor cryptor = new(secret);

CommonHeader headerA;
using (MemoryStream stream = new(buffer))
using (BigEndianBinaryReader reader = new(stream))
{
    headerA = CommonHeader.Parse(reader);
}

using (MemoryStream stream = new())
using (BigEndianBinaryWriter writer = new(stream))
{
    headerA.Flags = 0;
    cryptor!.EncryptMessage(writer, headerA!, new ICdpWriteable[]
    {
        new ConnectionHeader()
        {
            ConnectionMode = ConnectionMode.Proximal,
            ConnectMessageType = ConnectionType.DeviceAuthResponse
        },
        AuthenticationPayload.Create(
            localEncryption.DeviceCertificate!,
            localEncryption.Nonce, new(remoteNonce)
        )
    });
    buffer = stream.ToArray();
}

using (MemoryStream stream = new(buffer))
using (BigEndianBinaryReader reader = new(stream))
{
    if (!CommonHeader.TryParse(reader, out var header, out _) || header == null)
        throw new InvalidDataException();

    HandleMessage(header, cryptor.Read(reader, header));
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
            case ConnectionType.DeviceAuthResponse:
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