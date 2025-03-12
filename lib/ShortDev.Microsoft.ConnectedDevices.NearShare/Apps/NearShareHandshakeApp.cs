using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Serialization;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare.Apps;

public class NearShareHandshakeApp(ConnectedDevicesPlatform cdp) : CdpAppBase, ICdpAppId
{
    public static string Id { get; } = "0D472C30-80B5-4722-A279-0F3B97F0DCF2";

    public static string Name { get; } = "NearSharePlatform";

    public required NearShareReceiver Receiver { get; init; }

    public override void HandleMessage(CdpMessage msg)
    {
        msg.ReadBinary(out ValueSet payload, out _);

        string id = payload.Get<Guid>("OperationId").ToString();
        cdp.RegisterApp(
            id,
            NearShareApp.Name,
            cdp => new NearShareApp(cdp)
            {
                Id = id,
                Receiver = Receiver
            }
        );

        ValueSet response = new();
        response.Add("SelectedPlatformVersion", 1u);
        response.Add("VersionHandShakeResult", 1u);
        SendValueSet(response, msgId: 0);

        Channel.Dispose(closeSession: false, closeSocket: false);
    }
}
