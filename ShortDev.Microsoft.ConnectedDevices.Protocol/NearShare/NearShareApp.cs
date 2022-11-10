using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Serialization;
using ShortDev.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.NearShare;

public class NearShareApp : ICdpApp
{
    public static string Name { get; } = "NearSharePlatform";

    public required ICdpPlatformHandler PlatformHandler { get; init; }
    public required string Id { get; init; }

    const uint PartitionSize = 1024u; // 131072u

    public void HandleMessage(CdpChannel channel, CdpMessage msg)
    {
        bool expectMessage = true;

        CommonHeader header = msg.Header;
        BinaryReader payloadReader = msg.Read();

        var prepend = payloadReader.ReadBytes(0x0000000C);
        var buffer = payloadReader.ReadPayload();
        Debug.Print(BinaryConvert.ToString(buffer));
        var payload = ValueSet.Parse(buffer);

        header.AdditionalHeaders.RemoveAll((x) => x.Type == AdditionalHeaderType.CorrelationVector);

        if (header.HasFlag(MessageFlags.ShouldAck))
            channel.SendAck(header);

        ValueSet response = new();

        if (payload.ContainsKey("ControlMessage"))
        {
            var msgType = (ControlMessageType)payload.Get<uint>("ControlMessage");
            switch (msgType)
            {
                case ControlMessageType.StartRequest:
                    {
                        var dataKind = (DataKind)payload.Get<uint>("DataKind");
                        if (dataKind == DataKind.File)
                        {
                            var fileNames = payload.Get<List<string>>("FileNames");
                            if (fileNames.Count != 1)
                                throw new NotImplementedException("Only able to receive one file at a time");

                            PlatformHandler.Log(0, $"Receiving file \"{fileNames[0]}\" from session {header.SessionId.ToString("X")}");

                            var bytesToSend = payload.Get<ulong>("BytesToSend");
                            for (uint requestedPosition = 0; requestedPosition < bytesToSend; requestedPosition += PartitionSize)
                            {
                                ValueSet request = new();
                                request.Add("BlobPosition", (ulong)requestedPosition);
                                request.Add("BlobSize", PartitionSize);
                                request.Add("ContentId", 0u);
                                request.Add("ControlMessage", (uint)ControlMessageType.FetchDataRequest);

                                header.Flags = 0;
                                channel.SendMessage(header, (payloadWriter) =>
                                {
                                    payloadWriter.Write(prepend);
                                    request.Write(payloadWriter);
                                });
                            }

                            return;
                        }
                        else if (dataKind == DataKind.Uri)
                        {
                            var uri = payload.Get<string>("Uri");
                            PlatformHandler.Log(0, $"Received uri \"{uri}\" from session {header.SessionId.ToString("X")}");
                            PlatformHandler.LaunchUri(uri);
                            expectMessage = false;
                        }
                        else
                            throw new NotImplementedException($"DataKind {dataKind} not implemented");
                        break;
                    }
                case ControlMessageType.FetchDataResponse:
                    {
                        expectMessage = true;
                        break;
                    }
            }
        }
        else
            expectMessage = false;

        if (!expectMessage)
        {
            // Finished
            response.Add("ControlMessage", (uint)ControlMessageType.StartResponse);
            channel.Session.Dispose();
            channel.Dispose();

            CdpAppRegistration.UnregisterApp(Id, Name);
        }

        header.Flags = 0;
        channel.SendMessage(header, (payloadWriter) =>
        {
            payloadWriter.Write(prepend);
            response.Write(payloadWriter);
        });
    }

    public void Dispose() { }
}
