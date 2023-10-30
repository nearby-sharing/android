using ShortDev.Microsoft.ConnectedDevices.NearShare.Internal;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public sealed class NearShareReceiver
{
    public static void Register(ConnectedDevicesPlatform platform)
    {
        if (!platform.IsAdvertising)
            throw new InvalidOperationException($"\"{platform}\" is not advertising!");
        if (!platform.IsListening)
            throw new InvalidOperationException($"\"{platform}\" is not listening!");

        CdpAppRegistration.RegisterApp<NearShareHandshakeApp>(cdp => new(cdp));
    }


    public static event Action<UriTransferToken>? ReceivedUri;
    internal static void OnReceivedUri(UriTransferToken token)
        => ReceivedUri?.Invoke(token);

    public static event Action<FileTransferToken>? FileTransfer;
    internal static void OnFileTransfer(FileTransferToken token)
        => FileTransfer?.Invoke(token);

    public static void Unregister()
    {
        ReceivedUri = null;
        FileTransfer = null;

        CdpAppRegistration.TryUnregisterApp<NearShareHandshakeApp>();
    }
}
