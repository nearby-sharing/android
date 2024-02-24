﻿using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms;

/// <summary>
/// Provides direct low-level access to inter-device communication.
/// </summary>
public sealed class CdpSocket : IDisposable
{
    public required CdpTransportType TransportType { get; init; }
    public required Stream InputStream { get; init; }
    public required Stream OutputStream { get; init; }

    public void SendData(EndianWriter writer)
    {
        lock (OutputStream)
            writer.CopyTo(OutputStream);
    }

    public required CdpDevice RemoteDevice { get; init; }

    public bool IsClosed { get; private set; }
    public Action? Close { private get; set; }

    public event Action? Disposed;

    public void Dispose()
    {
        if (Close == null)
            throw new InvalidOperationException("No close handler has been registered");

        if (IsClosed)
            return;

        Close();

        IsClosed = true;

        Disposed?.Invoke();
    }
}
