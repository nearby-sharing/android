using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public sealed class CdpFileProvider : IDisposable
{
    readonly Stream _buffer;
    private CdpFileProvider(string fileName, Stream buffer)
    {
        FileName = SanitizeFileName(fileName);
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

    public void ReadBlob(ulong start, Span<byte> buffer)
    {
        _buffer.Position = (long)start;
        _buffer.ReadExactly(buffer);
    }

    public void Dispose()
    {
        _buffer.Dispose();
    }

    private static string SanitizeFileName(string value)
    {
        value = Path.GetFileName(value);
        return string.Create(length: value.Length, value.AsSpan(), static (dest, src) =>
        {
            for (int i = 0; i < dest.Length; i++)
            {
                dest[i] = src[i] switch
                {
                    (>= (char)1 and <= (char)31) or
                    '\"' or '<' or '>' or '|' or '\0' or
                    ':' or '*' or '?' or '\\' or '/' => '_',
                    var c => c,
                };
            }
        });
    }
}
