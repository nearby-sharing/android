using ShortDev.Networking;
using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;

/// <summary>
/// Provides direct low-level access to inter-device communication.
/// </summary>
public sealed class CdpSocket : IDisposable
{
    public required Stream InputStream { get; set; }
    public required Stream OutputStream { get; set; }

    BinaryReader? _readerCache;
    public BinaryReader Reader
        => _readerCache ??= new BigEndianBinaryReader(InputStream);

    BinaryWriter? _writerCache;
    public BinaryWriter Writer
        => _writerCache ??= new BigEndianBinaryWriter(OutputStream);

    public required CdpDevice RemoteDevice { get; set; }

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
