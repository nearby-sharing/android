using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;
using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Networking;
using Spectre.Console;
using System.Diagnostics;
using System.IO.Pipes;
using Windows.Foundation.Collections;
using Windows.System;

//var adapter = await BluetoothAdapter.GetDefaultAsync();
//Debug.Print(adapter.BluetoothAddress.ToString("X"));

//NamedPipeClientStream clientStream = new("\\\\.\\pipe\\CDPInOut");
//await clientStream.ConnectAsync();

NamedPipeServerStream pipeServer = new("CDPInOut", PipeDirection.InOut);
await pipeServer.WaitForConnectionAsync();
Console.WriteLine("Client connected!");
StreamReader pipeReader = new(pipeServer);
while (true)
{
    Console.WriteLine(pipeReader.ReadToEndAsync());
}

return;

var requestData = "000001000000000000002B2D120A040E43006F006E00740072006F006C004D00650073007300610067006500100AC5680600124D006100780050006C006100740066006F0072006D00560065007200730069006F006E00100AC5680100124D0069006E0050006C006100740066006F0072006D00560065007200730069006F006E00100AC56801000B4F007000650072006100740069006F006E0049006400101ECB720A0105BA8FA2D70D248BC70244A296016699F198A8BDC8EAF10A000000";
// Response: 2d120a0217530065006c006500630074006500640050006c006100740066006f0072006d00560065007200730069006f006e00100ac568010016560065007200730069006f006e00480061006e0064005300680061006b00650052006500730075006c007400100ac5680100000000000000000000000000


// var schema = Schema<CdpCryptor>.RuntimeSchema.SchemaDef;
var bondReader = new CompactBinaryReader<InputStream>(new InputStream(new MemoryStream(BinaryConvert.ToBytes(requestData))));
// Bond.Deserialize<CompactBinaryReader<OutputStream>>.From<IDictionary<>
var bondWriter = new CompactBinaryCounter();

// Bond.Deserialize<CompactBinaryReader<Bond.IO.Unsafe.InputStream>>.

while (true)
{
    var headerBuffer = BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Message"));
    using (MemoryStream stream = new(headerBuffer))
    using (BigEndianBinaryReader reader = new(stream))
    {
        var headerB = CommonHeader.Parse(reader);
        if (headerB.Type != MessageType.Connect)
        {
            Debugger.Break();
        }
    }
}

return;

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