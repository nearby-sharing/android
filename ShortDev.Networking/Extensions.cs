using System.IO;
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

    #region BinaryWriter
    public static void WriteWithLength(this BinaryWriter @this, string value)
        => @this.WriteWithLength(value, DefaultEncoding);

    public static void WriteWithLength(this BinaryWriter @this, string value, Encoding encoding)
        => @this.WriteWithLength(encoding.GetBytes(value));

    public static void WriteWithLength(this BinaryWriter @this, byte[] value)
    {
        @this.Write((ushort)value.Length);
        @this.Write(value);
    }
    #endregion
}
