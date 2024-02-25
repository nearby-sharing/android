using System;

namespace ShortDev.Microsoft.ConnectedDevices.Exceptions;

public class CdpException(string msg) : Exception(msg)
{
}
