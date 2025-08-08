using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.NearShare;

namespace NearShare.ViewModels;

partial class SendDataViewModel
{

    public static SendDataViewModel SendFiles(NearShareSender sender, CdpDevice device, IEnumerable<CdpFileProvider> files)
    {
        CancellationTokenSource cancellationTokenSource = new();
        Progress<NearShareProgress>? progress = new();
        return new(
            sender,
            device,
            taskFactory: () => sender.SendFilesAsync(device, [.. files], progress, cancellationTokenSource.Token),
            cancellationTokenSource,
            progress
        );
    }

    public static SendDataViewModel SendUri(NearShareSender sender, CdpDevice device, Uri uri)
    {
        CancellationTokenSource cancellationTokenSource = new();
        return new(
            sender,
            device,
            taskFactory: () => sender.SendUriAsync(device, uri, cancellationTokenSource.Token),
            cancellationTokenSource,
            progress: null
        );
    }

}
