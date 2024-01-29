using System.Collections;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public abstract class TransferToken
{
    public required string DeviceName { get; init; }
}

public sealed class UriTransferToken : TransferToken
{
    public required string Uri { get; init; }
}

public readonly record struct FileShareInfo(uint Id, string Name, ulong Size);

public sealed class FileTransferToken : TransferToken, IEnumerable<FileShareInfo>
{
    public uint TotalFiles => (uint)Files.Count;
    public required ulong TotalBytes { get; init; }
    public required IReadOnlyList<FileShareInfo> Files { get; init; }

    public IEnumerator<FileShareInfo> GetEnumerator() => Files.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #region Formatting
    public static string FormatFileSize(ulong fileSize)
    {
        if (fileSize > Constants.GB)
            return $"{CalcSize(fileSize, Constants.GB)} GB";

        if (fileSize > Constants.MB)
            return $"{CalcSize(fileSize, Constants.MB)} MB";

        if (fileSize > Constants.KB)
            return $"{CalcSize(fileSize, Constants.KB)} KB";

        return $"{fileSize} B";
    }

    static decimal CalcSize(ulong size, uint unit)
        => Math.Round((decimal)size / unit, 2);
    #endregion

    #region Acceptance
    readonly TaskCompletionSource<IReadOnlyList<Stream>> _acceptPromise = new();
    internal async ValueTask AwaitAcceptance()
        => await _acceptPromise.Task;

    public bool IsAccepted
        => _acceptPromise.Task.IsCompletedSuccessfully;

    public void Accept(IReadOnlyList<Stream> streams)
    {
        if (streams.Count != TotalFiles)
            throw new ArgumentException("Invalid number of streams", nameof(streams));

        _acceptPromise.SetResult(streams);
    }

    internal Stream GetStream(uint contentId)
    {
        for (int i = 0; i < Files.Count; i++)
        {
            if (Files[i].Id != contentId)
                continue;

            return _acceptPromise.Task.Result[i];
        }

        throw new ArgumentOutOfRangeException(nameof(contentId));
    }
    #endregion


    #region Cancellation
    readonly CancellationTokenSource _cancellationSource = new();

    public CancellationToken CancellationToken => _cancellationSource.Token;
    public void Cancel()
    {
        _acceptPromise.TrySetCanceled();
        _cancellationSource.Cancel();
    }
    #endregion


    #region Progress
    public bool IsTransferComplete { get; private set; }

    public event Action<NearShareProgress>? Progress;

    ulong _transferedBytes = 0;
    internal void SendProgressEvent(ulong byteTransferDelta)
    {
        _transferedBytes += byteTransferDelta;
        NearShareProgress progress = new()
        {
            TransferedBytes = _transferedBytes,
            TotalBytes = TotalBytes,
            TotalFiles = TotalFiles,
        };
        IsTransferComplete = progress.TransferedBytes >= progress.TotalBytes;
        _ = Task.Run(() => Progress?.Invoke(progress));
    }
    #endregion

    public event Action? Finished;
    internal void OnFinish()
    {
        _ = Task.Run(() => Finished?.Invoke());
    }
}
