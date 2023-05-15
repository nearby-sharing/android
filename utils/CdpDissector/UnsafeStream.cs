namespace CdpDissector;

internal sealed unsafe class UnsafeStream : Stream
{
    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    int _position = 0;
    public override long Position
    {
        get => _position;
        set => throw new NotImplementedException();
    }

    public required FieldInfo FieldInfo { get; init; }

    public override long Length
        => FieldInfo.length;

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override int Read(Span<byte> buffer)
    {
        var length = buffer.Length;

        if (_position + length >= Length)
            throw new IOException();

        for (int i = 0; i < length; i++)
        {
            buffer[i] = LibWireShark.tvb_get_guint8(FieldInfo.dsTvb, (int)FieldInfo.start + _position + i);
        }

        _position += length;
        return length;
    }

    #region NotImplemented
    public override void Flush()
        => throw new NotImplementedException();

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotImplementedException();

    public override void SetLength(long value)
        => throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotImplementedException();
    #endregion
}
