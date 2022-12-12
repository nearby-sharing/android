namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Exceptions;

public sealed class CdpProtocolException : CdpException
{
    public CdpProtocolException(string msg) : base(msg) { }
}
