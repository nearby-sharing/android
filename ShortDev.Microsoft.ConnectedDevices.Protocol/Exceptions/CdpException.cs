using System;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Exceptions;

public class CdpException : Exception
{
    public CdpException(string msg) : base(msg) { }
}
