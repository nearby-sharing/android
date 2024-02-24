namespace ShortDev.Microsoft.ConnectedDevices.Messages;

/// <summary>
///  Indicates the status of connection / authentication 
/// </summary>
public enum CdpResult : byte
{
    Success,
    Pending,
    Failure_Authentication,
    Failure_NotAllowed,
    Failure_Unknown
}
