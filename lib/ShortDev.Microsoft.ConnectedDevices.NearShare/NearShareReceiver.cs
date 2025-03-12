using ShortDev.Microsoft.ConnectedDevices.NearShare.Apps;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public sealed class NearShareReceiver : IDisposable
{
    public ConnectedDevicesPlatform Platform { get; }
    public NearShareReceiver(ConnectedDevicesPlatform platform)
    {
        Platform = platform;

        platform.RegisterApp<NearShareHandshakeApp>(cdp => new(cdp) { Receiver = this });
    }

    public event Action<UriTransferToken>? ReceivedUri;
    internal void OnReceivedUri(UriTransferToken token)
        => ReceivedUri?.Invoke(token);

    public event Action<FileTransferToken>? FileTransfer;
    internal void OnFileTransfer(FileTransferToken token)
        => FileTransfer?.Invoke(token);

    public void Dispose()
    {
        ReceivedUri = null;
        FileTransfer = null;

        Platform.TryUnregisterApp<NearShareHandshakeApp>();
    }
}
