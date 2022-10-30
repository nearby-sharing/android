using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Networking;
using Spectre.Console;

//var adapter = await BluetoothAdapter.GetDefaultAsync();
//Debug.Print(adapter.BluetoothAddress.ToString("X"));

//var requestData = /* 00000100000000000000 */ "2B2D120A040E43006F006E00740072006F006C004D00650073007300610067006500100AC5680600124D006100780050006C006100740066006F0072006D00560065007200730069006F006E00100AC5680100124D0069006E0050006C006100740066006F0072006D00560065007200730069006F006E00100AC56801000B4F007000650072006100740069006F006E0049006400101ECB720A0105BA8FA2D70D248BC70244A296016699F198A8BDC8EAF10A000000";
//// Response: 2d120a0217530065006c006500630074006500640050006c006100740066006f0072006d00560065007200730069006f006e00100ac568010016560065007200730069006f006e00480061006e0064005300680061006b00650052006500730075006c007400100ac5680100000000000000000000000000


//// var schema = Schema<CdpCryptor>.RuntimeSchema.SchemaDef;
//var bondReader = new CompactBinaryReader<InputStream>(new InputStream(new MemoryStream(BinaryConvert.ToBytes(requestData))), 2);
//bondReader.ReadStructBegin();
//bondReader.ReadFieldBegin(out var fieldType2, out var fieldId2);
//bondReader.ReadContainerBegin(out var count, out var keyType, out var valueType);
//for (int i = 0; i < count; i++)
//{
//    var str = bondReader.ReadWString();
//    // bondReader.ReadBytes(1);
//    var abc = bondReader.ReadInt32();
//}

var secret = BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Secret"));
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
        }
    }
    else
    {
        Console.WriteLine();
        Console.WriteLine(BinaryConvert.ToString(reader.ReadPayload()));
    }
}