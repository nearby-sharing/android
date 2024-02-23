using System;

namespace ShortDev.Networking;

public static class BinaryConvert
{
    // https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array
    public static void AsBytes(string hex, out int length, Span<byte> buffer)
    {
        ArgumentNullException.ThrowIfNull(hex);

        if (hex.Length % 2 == 1)
            throw new ArgumentException("Invalid hex string", nameof(hex));

        length = hex.Length >> 1;

        if (buffer == null)
            return;

        for (int i = 0; i < hex.Length >> 1; i++)
            buffer[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
    }

    public static byte[] ToBytes(string hex)
    {
        AsBytes(hex, out var length, null);
        byte[] buffer = new byte[length];
        AsBytes(hex, out _, buffer);
        return buffer;
    }

    static int GetHexVal(char hex)
    {
        int val = hex;
        //For uppercase A-F letters:
        //return val - (val < 58 ? 48 : 55);
        //For lowercase a-f letters:
        //return val - (val < 58 ? 48 : 87);
        //Or the two combined, but a bit slower:
        return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
    }

    public static string ToString(byte[] data)
        => BitConverter.ToString(data).Replace("-", "");

    public static byte[] ToReversed(ReadOnlySpan<byte> data)
    {
        var result = new byte[data.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = data[result.Length - i - 1];
        return result;
    }
}
