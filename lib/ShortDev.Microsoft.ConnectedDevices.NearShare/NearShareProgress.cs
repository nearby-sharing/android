namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public sealed class NearShareProgress
{
    public required ulong TransferedBytes { get; init; }
    public required ulong TotalBytes { get; init; }
    public required uint TotalFiles { get; init; }
}
