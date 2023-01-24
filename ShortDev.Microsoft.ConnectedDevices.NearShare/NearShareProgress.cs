namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public sealed class NearShareProgress
{
    public required long BytesSent { get; init; }
    public required int FilesSent { get; init; }
    public required long TotalBytesToSend { get; init; }
    public required int TotalFilesToSend { get; init; }
}
