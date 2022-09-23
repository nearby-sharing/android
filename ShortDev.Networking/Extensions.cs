using System.IO;
using System.Text;

namespace ShortDev.Networking;

public static class Extensions
{
    static readonly Encoding DefaultEncoding = Encoding.UTF8;

    #region BinaryReader
    public static string ReadStringWithLength(this BinaryReader @this)
        => @this.ReadStringWithLength(DefaultEncoding);

    public static string ReadStringWithLength(this BinaryReader @this, Encoding encoding)
        => encoding.GetString(@this.ReadBytesWithLength());

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
