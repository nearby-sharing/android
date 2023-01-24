using ShortDev.Microsoft.ConnectedDevices.Platforms;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public sealed class NearShareSender
{
    public ConnectedDevicesPlatform Platform { get; }
    public NearShareSender(ConnectedDevicesPlatform platform)
    {
        Platform = platform;
    }

    public async Task SendUriAsync(CdpDevice device, Uri uri)
    {
        var session = await Platform.ConnectAsync(device);

    }

    public Task SendFileAsync(CdpDevice device, CdpFileProvider file, IProgress<NearShareProgress> progress, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task SendFilesAsync(CdpDevice device, CdpFileProvider[] files, IProgress<NearShareProgress> progress, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
