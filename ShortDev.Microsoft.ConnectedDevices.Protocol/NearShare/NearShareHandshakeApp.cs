using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Serialization;
using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.NearShare;

public class NearShareHandshakeApp : ICdpApp, ICdpAppId
{
    public static string Id { get; } = "0D472C30-80B5-4722-A279-0F3B97F0DCF2";

    public static string Name { get; } = "NearSharePlatform";

    public required ICdpPlatformHandler PlatformHandler { get; init; }

    public void HandleMessage(CdpRfcommSocket socket, CommonHeader header, BinaryReader payloadReader, BinaryWriter payloadWriter, ref bool expectMessage)
    {
        var prepend = payloadReader.ReadBytes(0x0000000C);
        var payload = ValueSet.Parse(payloadReader.ReadPayload());
        header.AdditionalHeaders.RemoveAll((x) => x.Type == AdditionalHeaderType.CorrelationVector);

        CdpAppRegistration.RegisterApp(
            payload.Get<Guid>("OperationId").ToString(), 
            NearShareApp.Name, 
            () => new NearShareApp() { PlatformHandler = PlatformHandler }
        );

        ValueSet response = new();
        response.Add("SelectedPlatformVersion", 1u);
        response.Add("VersionHandShakeResult", 1u);

        payloadWriter.Write(prepend);
        response.Write(payloadWriter);
    }
}
