namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public abstract class TransferToken
{
    public required string DeviceName { get; init; }
}

public sealed class UriTransferToken : TransferToken
{
    public required string Uri { get; init; }
}

public abstract class FileTransferToken : TransferToken
{
    public required IReadOnlyList<string> FileNames { get; init; }

    public required ulong TotalBytesToSend { get; init; }


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
    readonly TaskCompletionSource<FileStream[]> _promise = new();
    internal Task TaskInternal
        => _promise.Task;

    internal FileStream[] StreamS
        => _promise.Task.Result;

    public bool IsAccepted { get; private set; }

    public void Accept(FileStream[] fileStream)
    {
        _promise.SetResult(fileStream);
        IsAccepted = true;
    }

    public void Cancel()
        => _promise.TrySetCanceled();
    #endregion


    #region Progress
    public bool IsTransferComplete { get; internal set; }

    public event Action<NearShareProgress>? Progress;

    public void SetProgressListener(Action<NearShareProgress> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        Progress = listener;
    }

    protected void OnProgress(NearShareProgress args)
    {
        IsTransferComplete = args.BytesSent >= args.TotalBytesToSend;
        _ = Task.Run(() => Progress?.Invoke(args));
    }
    #endregion
}

internal sealed class FileTransferTokenImpl : FileTransferToken
{
    public required ulong[] ContentSizes { get; init; } = Array.Empty<ulong>();
    public required uint[] ContentIds { get; init; } = Array.Empty<uint>();
    public required uint TotalFilesToSend { get; init; }

    public ulong BytesSent { get; set; }
    public uint FilesSent { get; set; }

    public void SendProgressEvent()
        => OnProgress(new()
        {
            BytesSent = BytesSent,
            FilesSent = FilesSent,
            TotalBytesToSend = TotalBytesToSend,
            TotalFilesToSend = TotalFilesToSend,
        });
}
