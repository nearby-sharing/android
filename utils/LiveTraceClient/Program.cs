using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using ShortDev.IO;
using ShortDev.IO.Input;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.Authentication;

const string pipeName = "CDPInOut";

// Allow CdpSvc to access the pipe
PipeSecurity pipeSecurity = new();
pipeSecurity.AddAccessRule(new(
    new SecurityIdentifier(WellKnownSidType.LocalServiceSid, domainSid: null),
    PipeAccessRights.FullControl,
    AccessControlType.Allow
));

// Create the pipe
using NamedPipeServerStream pipeServer = NamedPipeServerStreamAcl.Create(
    pipeName,
    PipeDirection.InOut,
    maxNumberOfServerInstances: 1,
    PipeTransmissionMode.Byte,
    PipeOptions.Asynchronous,
    inBufferSize: 0,
    outBufferSize: 0,
    pipeSecurity
);

Console.WriteLine("Waiting for system service to connect...");
await pipeServer.WaitForConnectionAsync();
Console.WriteLine("Service connected.");

var reader = EndianReader.FromStream(Endianness.BigEndian, stream: pipeServer);

while (true)
{
    Console.WriteLine();
    Console.WriteLine("--------------------------------");
    Console.WriteLine();

    reader.ReadUInt8(); // Allways "1"
    reader.ReadUInt8(); // Bool

    var endpointLength = (int)reader.ReadUInt32();
    byte[] endpoint = new byte[endpointLength];
    reader.ReadBytes(endpoint);
    Console.WriteLine(ParseEndpoint(endpoint).ToString());

    var msgLength = (int)reader.ReadUInt32();
    byte[] message = new byte[msgLength];
    reader.ReadBytes(message);
    HandleMessage(message);
}

static EndpointInfo ParseEndpoint(ReadOnlySpan<byte> endpointData)
{
    var reader = EndianReader.FromSpan(Endianness.BigEndian, endpointData);
    var address = reader.ReadStringWithLength();
    var service = reader.ReadStringWithLength();
    var type = reader.ReadUInt16();
    return new((CdpTransportType)type, address, service);
}

static void HandleMessage(ReadOnlySpan<byte> message)
{
    var reader = EndianReader.FromSpan(Endianness.BigEndian, message);
    if (!CommonHeader.TryParse(ref reader, out var header, out var ex))
        throw ex;

    if (header.Type == MessageType.Session)
    {
        Console.WriteLine("Session message - skipping detailed parsing.");
        return;
    }

    Console.WriteLine($"Type={header.Type}, Flags={header.Flags}, SequenceNumber={header.SequenceNumber}, RequestID={header.RequestID}, FragmentIndex={header.FragmentIndex}, FragmentCount={header.FragmentCount}, SessionId={header.SessionId}, ChannelId={header.ChannelId}");
    foreach (var additionalHeader in header.AdditionalHeaders)
    {
        Console.WriteLine($" - {additionalHeader.Type}: {Convert.ToHexString(additionalHeader.Value.Span)}");
    }

    switch (header.Type)
    {
        case MessageType.Connect:
            var connectHeader = ConnectionHeader.Parse(ref reader);
            Console.WriteLine(connectHeader.ToString());
            switch (connectHeader.MessageType)
            {
                case ConnectionType.ConnectRequest:
                    var connectRequest = ConnectionRequest.Parse(ref reader);
                    Console.WriteLine(connectRequest.ToString());
                    break;

                case ConnectionType.ConnectResponse:
                    var connectResponse = ConnectionResponse.Parse(ref reader);
                    Console.WriteLine(connectResponse.ToString());
                    break;

                case ConnectionType.DeviceInfoMessage:
                    var deviceInfoMessage = DeviceInfoMessage.Parse(ref reader);
                    Console.WriteLine(deviceInfoMessage.ToString());
                    break;

                case ConnectionType.UpgradeRequest:
                    var upgradeRequest = UpgradeRequest.Parse(ref reader);
                    Console.WriteLine(upgradeRequest.ToString());
                    break;

                case ConnectionType.UpgradeResponse:
                    var upgradeResponse = UpgradeResponse.Parse(ref reader);
                    Console.WriteLine(upgradeResponse.ToString());
                    break;

                case ConnectionType.UpgradeFinalization:
                    var upgradeFinalization = EndpointMetadata.ParseArray(ref reader);
                    Console.WriteLine(upgradeFinalization.ToString());
                    break;

                case ConnectionType.UpgradeFailure:
                    var hresultPayload = HResultPayload.Parse(ref reader);
                    Console.WriteLine(hresultPayload.ToString());
                    break;

                case ConnectionType.DeviceAuthRequest:
                case ConnectionType.DeviceAuthResponse:
                case ConnectionType.UserDeviceAuthRequest:
                case ConnectionType.UserDeviceAuthResponse:
                    var userDeviceAuthRequest = AuthenticationPayload.Parse(ref reader);
                    Console.WriteLine(userDeviceAuthRequest.ToString());
                    break;

                case ConnectionType.TransportRequest:
                    var transportRequest = UpgradeIdPayload.Parse(ref reader);
                    Console.WriteLine(transportRequest.ToString());
                    break;
            }
            break;

        case MessageType.Control:
            var controlHeader = ControlHeader.Parse(ref reader);
            Console.WriteLine(controlHeader.ToString());
            switch (controlHeader.MessageType)
            {
                case ControlMessageType.StartChannelRequest:
                    var startChannelRequest = StartChannelRequest.Parse(ref reader);
                    Console.WriteLine(startChannelRequest.ToString());
                    break;

                case ControlMessageType.StartChannelResponse:
                    var startChannelResponse = StartChannelResponse.Parse(ref reader);
                    Console.WriteLine(startChannelResponse.ToString());
                    break;
            }
            break;
    }
}
