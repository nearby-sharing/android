using System;

namespace ShortDev.Networking
{
    public static class BinaryConvert
    {
        // https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array
        public static void AsBytes(string hex, out int length, Span<byte> buffer)
        {
            if (hex == null)
                throw new ArgumentNullException(nameof(hex));

            if (hex.Length % 2 == 1)
                throw new ArgumentException("Invalid hex string", nameof(hex));

            length = hex.Length >> 1;

            if (buffer == null)
                return;

            for (int i = 0; i < hex.Length >> 1; i++)
                buffer[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
        }

        public static int GetHexVal(char hex)
        {
            int val = hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}
