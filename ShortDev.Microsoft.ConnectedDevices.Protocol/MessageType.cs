namespace ShortDev.Microsoft.ConnectedDevices.Protocol
{
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
}
