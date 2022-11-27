using System;
using System.IO;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.NearShare;

public interface INearSharePlatformHandler
{
    void Log(int level, string message);
    void OnReceivedUri(UriTranferToken transfer);
    void OnFileTransfer(FileTransferToken transfer);
}

public abstract class TranferToken
{
    public required string DeviceName { get; init; }
}

public sealed class UriTranferToken : TranferToken
{
    public required string Uri { get; init; }
}

public sealed class FileTransferToken : TranferToken
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
    TaskCompletionSource<FileStream> _promise = new();
    internal Task WaitForAcceptance()
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
