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

    public void HandleMessage(CdpRfcommSocket socket, CommonHeader header, BinaryReader payloadReader, BinaryWriter payloadWriter, ref bool expectMessage)
    {
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
                            response.Add("ControlMessage", (uint)ControlMessageType.StartResponse);
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
        {
            response.Add("ControlMessage", (uint)ControlMessageType.StartResponse);
        }

        payloadWriter.Write(prepend);
        response.Write(payloadWriter);
    }
}
