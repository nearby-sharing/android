using System;
using System.IO;
using System.Runtime.CompilerServices;
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
    public string FileSizeFormatted
    {
        get
        {
            if (FileSize > Constants.GB)
                return $"{CalcSize(FileSize, Constants.GB)} GB";

            if (FileSize > Constants.MB)
                return $"{CalcSize(FileSize, Constants.MB)} MB";

            if (FileSize > Constants.KB)
                return $"{CalcSize(FileSize, Constants.KB)} KB";

            return $"{FileSize} B";
        }
    }

    decimal CalcSize(ulong size, uint unit)
        => Math.Round((decimal)size / unit, 2);

    TaskCompletionSource<FileStream> _promise = new();
    internal TaskAwaiter<FileStream> GetAwaiter()
        => _promise.Task.GetAwaiter();

    public void Accept(FileStream fileStream)
        => _promise.SetResult(fileStream);

    public void Cancel()
        => _promise.SetCanceled();
}
