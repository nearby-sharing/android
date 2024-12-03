using System.Security.Cryptography;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;

public sealed record GroupInfo(string Ssid, ReadOnlyMemory<byte> PreSharedKey)
{
    public static GroupInfo Create(string ssid, string passphrase)
    {
        var ssidBytes = Encoding.ASCII.GetBytes(ssid);
        var preSharedKey = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt: ssidBytes, iterations: 4096, HashAlgorithmName.SHA1, outputLength: 32);
        return new(ssid, preSharedKey);
    }
}
