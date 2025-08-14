using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.IO.Pipes;
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

EndianReader reader = new(Endianness.BigEndian, stream: pipeServer);

while (true)
{
    Console.WriteLine();
    Console.WriteLine("--------------------------------");
    Console.WriteLine();

    reader.ReadByte(); // Allways "1"
    reader.ReadByte(); // Bool

    var endpointLength = (int)reader.ReadUInt32();
    var endpoint = reader.ReadBytes(endpointLength);
    Console.WriteLine(ParseEndpoint(endpoint).ToString());

    var msgLength = (int)reader.ReadUInt32();
    var message = reader.ReadBytes(msgLength);
    HandleMessage(message);
}

static EndpointInfo ParseEndpoint(ReadOnlySpan<byte> endpointData)
{
    EndianReader reader = new(Endianness.BigEndian, endpointData);
    var address = reader.ReadStringWithLength();
    var service = reader.ReadStringWithLength();
    var type = reader.ReadUInt16();
    return new((CdpTransportType)type, address, service);
}

static void HandleMessage(ReadOnlySpan<byte> message)
{
    EndianReader reader = new(Endianness.BigEndian, message);
    if (!CommonHeader.TryParse(ref reader, out var header, out var ex))
        throw ex;

    Console.WriteLine($"Type={header.Type}, Flags={header.Flags}, SequenceNumber={header.SequenceNumber}, RequestID={header.RequestID}, FragmentIndex={header.FragmentIndex}, FragmentCount={header.FragmentCount}, SessionId={header.SessionId}, ChannelId={header.ChannelId}");
}
