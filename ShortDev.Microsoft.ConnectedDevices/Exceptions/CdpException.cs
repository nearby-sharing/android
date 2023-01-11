using System;

namespace ShortDev.Microsoft.ConnectedDevices.Exceptions;

public class CdpException : Exception
{
    public CdpException(string msg) : base(msg) { }
}
