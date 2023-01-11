using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Networking;
using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms;

/// <summary>
/// Provides direct low-level access to inter-device communication.
/// </summary>
public sealed class CdpSocket : IDisposable
{
    public required CdpTransportType TransportType { get; init; }
    public required Stream InputStream { get; init; }
    public required Stream OutputStream { get; init; }

    BinaryReader? _readerCache;
    public BinaryReader Reader
        => _readerCache ??= new BigEndianBinaryReader(InputStream);

    BinaryWriter? _writerCache;
    public BinaryWriter Writer
        => _writerCache ??= new BigEndianBinaryWriter(OutputStream);

    public required CdpDevice RemoteDevice { get; init; }

    public bool IsClosed { get; private set; }
    public Action? Close { private get; set; }
    public void Dispose()
    {
        if (Close == null)
            throw new InvalidOperationException("No close handler has been registered");

        if (IsClosed)
            return;

        Close();
        _readerCache?.Dispose();
        _writerCache?.Dispose();

        IsClosed = true;
    }
}
