using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ShortDev.Networking;

public static class Extensions
{
    static readonly Encoding DefaultEncoding = Encoding.UTF8;

    #region BinaryReader
    public static string ReadStringWithLength(this BinaryReader @this, bool zeroByte = false)
        => @this.ReadStringWithLength(DefaultEncoding, zeroByte);

    public static string ReadStringWithLength(this BinaryReader @this, Encoding encoding, bool zeroByte = false)
    {
        var result = encoding.GetString(@this.ReadBytesWithLength());
        if (zeroByte)
            @this.ReadByte();
        return result;
    }

    public static byte[] ReadBytesWithLength(this BinaryReader @this)
    {
        var length = @this.ReadUInt16();
        return @this.ReadBytes(length);
    }
    #endregion

    public static Span<T> AsSpan<T>(this ReadOnlySpan<T> buffer)
        => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(buffer), buffer.Length);
}
