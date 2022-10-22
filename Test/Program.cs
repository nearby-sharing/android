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

using (MemoryStream stream = new(buffer))
using (BigEndianBinaryReader reader = new(stream))
{
    if (!CommonHeader.TryParse(reader, out var header, out _) || header == null)
        throw new InvalidDataException();

    if ((int)(header.Flags & MessageFlags.SessionEncrypted) > 0)
    {
        CdpCryptor encryptionHelper = new(secret);
        var payload = encryptionHelper.DecryptMessage(header, reader.ReadBytes(header.PayloadSize));
        using (MemoryStream payloadStream = new(payload))
        using (BigEndianBinaryReader payloadReader = new(payloadStream))
        {
            var payloadLength = payloadReader.ReadUInt32();
            if (payloadLength != payload.Length - 4)
                throw new InvalidDataException($"Expected payload to be {payloadLength} bytes long");

            HandleMessage(header, payloadReader);
        }
    }
    else
        HandleMessage(header, reader);
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
                    DeviceAuthenticationMessage msg = DeviceAuthenticationMessage.Parse(reader);
                    break;
                }
            default:
                throw new NotImplementedException();
        }
    }
    else
        throw new NotImplementedException();
}