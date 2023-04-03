namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public abstract class TransferToken
{
    public required string DeviceName { get; init; }
}

public sealed class UriTransferToken : TransferToken
{
    public required string Uri { get; init; }
}

public sealed class FileTransferToken : TransferToken
{
    public required string FileName { get; init; }

    public required ulong FileSize { get; init; }


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
    readonly TaskCompletionSource<FileStream> _promise = new();
    internal Task TaskInternal
        => _promise.Task;

    internal FileStream Stream
        => _promise.Task.Result;

    public bool IsAccepted { get; private set; }

    public void Accept(FileStream fileStream)
    {
        _promise.SetResult(fileStream);
        IsAccepted = true;
    }

    public void Cancel()
        => _promise.TrySetCanceled();
    #endregion


    #region Progress
    ulong _receivedBytes;
    public ulong ReceivedBytes
    {
        get => _receivedBytes;
        internal set
        {
            _receivedBytes = value;
            if (value >= FileSize)
                IsTransferComplete = true;

            Progress?.Invoke(this);
        }
    }

    public bool IsTransferComplete { get; private set; }

    public event Action<FileTransferToken>? Progress;

    public void SetProgressListener(Action<FileTransferToken> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        Progress = listener;
    }
    #endregion
}
