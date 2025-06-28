namespace ShortDev.Microsoft.ConnectedDevices.Transports;

/// <summary>
/// Provides direct low-level access to inter-device communication.
/// </summary>
public sealed class CdpSocket : IFragmentSender, IDisposable
{
    public CdpTransportType TransportType => Endpoint.TransportType;
    public required EndpointInfo Endpoint { get; init; }
    public required Stream InputStream { get; init; }
    public required Stream OutputStream { get; init; }

    public void SendFragment(ReadOnlySpan<byte> fragment)
    {
        OutputStream.Write(fragment);
        OutputStream.Flush();
    }

    public void SendFragment(ReadOnlySpan<byte> header, ReadOnlySpan<byte> payload)
    {
        lock (OutputStream)
        {
            OutputStream.Write(header);
            OutputStream.Write(payload);
            OutputStream.Flush();
        }
    }

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
