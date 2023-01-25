namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

public enum ChannelResult : byte
{
    Success,
    Failure_AccessDenied,
    Failure_NotFound,
    Failure_AlreadyOpen,
    Failure_Unknown
}
