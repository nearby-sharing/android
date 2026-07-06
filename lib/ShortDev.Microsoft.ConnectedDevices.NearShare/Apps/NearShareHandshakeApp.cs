using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
using ShortDev.Microsoft.ConnectedDevices.Serialization;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare.Apps;

public class NearShareHandshakeApp(CdpChannel channel) : CdpBondApp(channel), ICdpAppId
{
    public static string Id { get; } = "0D472C30-80B5-4722-A279-0F3B97F0DCF2";
    public static string Name { get; } = "NearSharePlatform";

    protected override void HandleMessage(in BinaryMsgHeader header, ValueSet payload)
    {
        string id = payload.Get<Guid>("OperationId").ToString();
        CdpAppRegistration.RegisterApp(
            id,
            NearShareApp.Name,
            channel => new NearShareApp(channel)
            {
                Id = id
            }
        );

        ValueSet response = new();
        response.Add("SelectedPlatformVersion", 1u);
        response.Add("VersionHandShakeResult", 1u);
        SendValueSet(response, messageId: 0);

        Channel.Dispose(closeSession: false, closeSocket: false);
    }
}
