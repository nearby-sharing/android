using ShortDev.Microsoft.ConnectedDevices.Protocol.Serialization;
using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.NearShare;

public class NearShareHandshakeApp : ICdpApp, ICdpAppId
{
    public static string Id { get; } = "0D472C30-80B5-4722-A279-0F3B97F0DCF2";

    public static string Name { get; } = "NearSharePlatform";

    public required INearSharePlatformHandler PlatformHandler { get; init; }

    public void HandleMessage(CdpChannel channel, CdpMessage msg)
    {
        CommonHeader header = msg.Header;
        BinaryReader payloadReader = msg.Read();

        var prepend = payloadReader.ReadBytes(0x0000000C);
        var payload = ValueSet.Parse(payloadReader.ReadPayload());
        header.AdditionalHeaders.RemoveAll((x) => x.Type == AdditionalHeaderType.CorrelationVector);

        string id = payload.Get<Guid>("OperationId").ToString();
        CdpAppRegistration.TryRegisterApp(
            id,
            NearShareApp.Name,
            () => new NearShareApp()
            {
                Id = id,
                PlatformHandler = PlatformHandler
            }
        );

        ValueSet response = new();
        response.Add("SelectedPlatformVersion", 1u);
        response.Add("VersionHandShakeResult", 1u);

        header.Flags = 0;
        channel.SendMessage(header, (payloadWriter) =>
        {
            payloadWriter.Write(prepend);
            response.Write(payloadWriter);
        });

        channel.Dispose();
    }

    public void Dispose() { }
}
