using ShortDev.Microsoft.ConnectedDevices.Platforms;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public interface INearSharePlatformHandler : ICdpPlatformHandler
{
    void OnReceivedUri(UriTransferToken transfer);
    void OnFileTransfer(FileTransferToken transfer);
}