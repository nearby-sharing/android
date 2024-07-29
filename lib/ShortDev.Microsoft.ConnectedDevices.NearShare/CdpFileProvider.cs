using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public sealed class CdpFileProvider : IDisposable
{
    readonly Stream _buffer;
    private CdpFileProvider(string fileName, Stream buffer)
    {
        FileName = fileName;
        _buffer = buffer;
    }

    public static CdpFileProvider FromContent(string fileName, string content)
        => FromContent(fileName, content, Encoding.UTF8);

    public static CdpFileProvider FromContent(string fileName, string content, Encoding encoding)
    {
        var buffer = encoding.GetBytes(content);
        return FromBuffer(fileName, buffer);
    }

    public static CdpFileProvider FromBuffer(string fileName, ReadOnlyMemory<byte> buffer)
    {
        MemoryStream stream = new();
        stream.Write(buffer.Span);
        return FromStream(fileName, stream);
    }

    public static CdpFileProvider FromStream(string fileName, Stream stream)
    {
        if (!stream.CanSeek)
            throw new ArgumentException("Stream can't seek", nameof(stream));
        if (!stream.CanRead)
            throw new ArgumentException("Stream can't read", nameof(stream));

        return new(fileName, stream);
    }

    public string FileName { get; }

    public ulong FileSize
        => (ulong)_buffer.Length;

    public ReadOnlyMemory<byte> ReadBlob(ulong start, uint length)
    {
        var buffer = new byte[length];
        _buffer.Position = (long)start;
        _buffer.Read(buffer);
        return buffer;
    }

    public void Dispose()
    {
        _buffer.Dispose();
    }
}
