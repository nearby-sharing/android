namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public sealed class NearShareProgress
{
    public required ulong BytesSent { get; init; }
    public required uint FilesSent { get; init; }
    public required ulong TotalBytesToSend { get; init; }
    public required uint TotalFilesToSend { get; init; }
}
