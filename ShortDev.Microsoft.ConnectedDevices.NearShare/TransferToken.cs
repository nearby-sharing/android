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

public record struct FileShareInfo(uint Id, string Name, ulong Size);

public sealed class FileTransferToken : TransferToken, IEnumerable<FileShareInfo>
{
    public required uint TotalFilesToSend { get; init; }
    public required ulong TotalBytesToSend { get; init; }
    public required IReadOnlyList<FileShareInfo> Files { get; init; }

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
    readonly TaskCompletionSource<IReadOnlyList<FileStream>> _promise = new();
    internal async ValueTask AwaitAcceptance()
        => await _promise.Task;

    public bool IsAccepted
        => _promise.Task.IsCompletedSuccessfully;

    public void Accept(IReadOnlyList<FileStream> fileStream)
    {
        if (fileStream.Count != TotalFilesToSend)
            throw new ArgumentException("Invalid number of streams", nameof(fileStream));

        _promise.SetResult(fileStream);
    }

    public void Cancel()
        => _promise.SetCanceled();

    internal FileStream GetStream(uint contentId)
    {
        for (int i = 0; i < Files.Count; i++)
        {
            if (Files[i].Id == contentId)
                return _promise.Task.Result[i];
        }

        throw new ArgumentOutOfRangeException(nameof(contentId));
    }
    #endregion


    #region Progress
    public bool IsTransferComplete { get; internal set; }

    public event Action<NearShareProgress>? Progress;

    internal ulong BytesSent { get; set; }
    internal uint FilesSent { get; set; }

    internal void SendProgressEvent()
    {
        NearShareProgress progress = new()
        {
            BytesSent = BytesSent,
            FilesSent = FilesSent,
            TotalBytesToSend = TotalBytesToSend,
            TotalFilesToSend = TotalFilesToSend,
        };
        IsTransferComplete = progress.BytesSent >= progress.TotalBytesToSend;
        _ = Task.Run(() => Progress?.Invoke(progress));
    }
    #endregion

    #region Info
    public IEnumerator<FileShareInfo> GetEnumerator()
        => Files.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
    #endregion

    internal void Close()
    {
        foreach (var stream in _promise.Task.Result)
        {
            stream.Flush();
            stream.Close();
            stream.Dispose();
        }
    }
}
