using System.Collections;
using System.Collections.Frozen;

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
    internal event Action<FileTransferToken>? Accepted;

    public bool IsAccepted => _streams is not null;

    FrozenDictionary<uint, Stream>? _streams;
    public void Accept(FrozenDictionary<uint, Stream> streams)
    {
        CancellationToken.ThrowIfCancellationRequested();

        foreach (var file in Files)
        {
            if (!streams.ContainsKey(file.Id))
                throw new ArgumentException($"Could not find stream for file '{file.Id}'", nameof(streams));
        }

        _streams = streams;

        Accepted?.Invoke(this);
    }

    internal Stream GetStream(uint contentId)
        => _streams?[contentId] ?? throw new InvalidOperationException("Transfer not accepted");
    #endregion


    #region Cancellation
    readonly CancellationTokenSource _cancellationSource = new();

    public CancellationToken CancellationToken => _cancellationSource.Token;
    public void Cancel()
        => _cancellationSource.Cancel();
    #endregion


    #region Progress
    public bool IsTransferComplete { get; private set; }

    public event Action<NearShareProgress>? Progress;

    ulong _transferedBytes = 0;
    internal void SendProgressEvent(ulong byteTransferDelta)
    {
        NearShareProgress progress = new()
        {
            TransferedBytes = Interlocked.Add(ref _transferedBytes, byteTransferDelta),
            TotalBytes = TotalBytes,
            TotalFiles = TotalFiles
        };
        IsTransferComplete = progress.TransferedBytes >= progress.TotalBytes;
        Task.Run(() => Progress?.Invoke(progress)).Forget();
    }
    #endregion

    public event Action? Finished;
    internal void OnFinish()
    {
        try
        {
            foreach (var stream in _streams?.Values ?? [])
            {
                stream.Flush();

                stream.Close();
                stream.Dispose();
            }
        }
        finally
        {
            Finished?.Invoke();
        }
    }
}
