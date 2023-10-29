namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public interface INearSharePlatformHandler
{
    void OnReceivedUri(UriTransferToken transfer);
    void OnFileTransfer(FileTransferToken transfer);
}