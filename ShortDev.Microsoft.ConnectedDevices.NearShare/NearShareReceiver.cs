using ShortDev.Microsoft.ConnectedDevices.NearShare.Internal;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public sealed class NearShareReceiver
{
    public static void Start(ConnectedDevicesPlatform platform, INearSharePlatformHandler handler)
    {
        if (!platform.IsAdvertising)
            throw new InvalidOperationException($"\"{platform}\" is not advertising!");
        if (!platform.IsListening)
            throw new InvalidOperationException($"\"{platform}\" is not listening!");

        CdpAppRegistration.RegisterApp<NearShareHandshakeApp>(() => new()
        {
            PlatformHandler = handler
        });
    }

    public static void Stop()
    {
        CdpAppRegistration.TryUnregisterApp<NearShareHandshakeApp>();
    }
}
