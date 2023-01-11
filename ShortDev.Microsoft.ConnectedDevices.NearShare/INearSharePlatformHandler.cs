using ShortDev.Microsoft.ConnectedDevices.Platforms;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public interface INearSharePlatformHandler : ICdpPlatformHandler
{
    void OnReceivedUri(UriTranferToken transfer);
    void OnFileTransfer(FileTransferToken transfer);
}