using ShortDev.IO;
using ShortDev.IO.Input;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

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

    if (header.Type == MessageType.Discovery)
    {
        Console.WriteLine("Discovery message - skipping detailed parsing.");
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

        case MessageType.Session:
            var msg = Session.GetMessage(header);
            msg.AddFragment(reader.Stream.ReadSlice((int)(reader.Stream.Length - reader.Stream.Position)));
            if (msg.IsComplete)
            {
                Console.WriteLine($"SessionId={header.SessionId:X}, ChannelId={header.ChannelId:X}");
                Console.WriteLine(Convert.ToHexString(msg.Content.Span));
            }
            break;
    }
}

sealed class Session
{
    private static readonly Dictionary<(uint sessionId, uint sequenceNumber, ulong requestId), CdpMessage> Messages = [];
    public static CdpMessage GetMessage(CommonHeader header)
    {
        var localSessionId = SessionId.Parse(header.SessionId).LocalSessionId;
        var sequenceNumber = header.SequenceNumber;
        var requestId = header.RequestID;

        return Messages.GetOrAdd((localSessionId, sequenceNumber, requestId), id => new(header));
    }
}

static class Extensions
{
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> @this, TKey key, Func<TKey, TValue> factory)
        where TKey : notnull
    {
        ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(@this, key, out var exists);
        if (!exists || value is null)
            value = factory(key);
        return value;
    }
}
