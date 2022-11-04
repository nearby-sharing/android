namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

/// <summary>
/// Indicates current message type. <br/>
/// (See <see cref="CommonHeader.Type"/>)
/// </summary>
public enum MessageType : byte
{
    None = 0,
    Discovery,
    Connect,
    Control,
    Session,
    Ack,
    ReliabilityResponse
}
