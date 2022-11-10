using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Serialization;
using ShortDev.Networking;
using System;
using System.Diagnostics;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.NearShare;

public class NearShareApp : ICdpApp
{
    public static string Name { get; } = "NearSharePlatform";

    public required ICdpPlatformHandler PlatformHandler { get; init; }
    public required string Id { get; init; }

    public bool HandleMessage(CdpSession session, CdpMessage msg, BinaryWriter payloadWriter)
    {
        bool expectMessage = true;

        CommonHeader header = msg.Header;
        BinaryReader payloadReader = msg.Read();

        var prepend = payloadReader.ReadBytes(0x0000000C);
        var buffer = payloadReader.ReadPayload();
        Debug.Print(BinaryConvert.ToString(buffer));
        var payload = ValueSet.Parse(buffer);

        header.AdditionalHeaders.RemoveAll((x) => x.Type == AdditionalHeaderType.CorrelationVector);

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
                            response.Add("BlobPosition", (ulong)0);
                            response.Add("BlobSize", 131072u);
                            response.Add("ContentId", 0u);
                            response.Add("ControlMessage", (uint)ControlMessageType.FetchDataRequest);
                        }
                        else if (dataKind == DataKind.Uri)
                        {
                            var uri = payload.Get<string>("Uri");
                            PlatformHandler?.Log(0, $"Received uri {uri} from session {header.SessionId.ToString("X")}");
                            PlatformHandler?.LaunchUri(uri);
                            expectMessage = false;
                        }
                        else
                            throw new NotImplementedException($"DataKind {dataKind} not implemented");
                        break;
                    }
                case ControlMessageType.FetchDataResponse:
                    {

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
            session.Dispose();

            CdpAppRegistration.UnregisterApp(Id, Name);
        }

        payloadWriter.Write(prepend);
        response.Write(payloadWriter);

        return expectMessage;
    }

    public void Dispose() { }
}
