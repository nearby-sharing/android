using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Networking;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.Authentication;

public sealed class DeviceAuthenticationMessage : ICdpPayload<DeviceAuthenticationMessage>
{
    public static DeviceAuthenticationMessage Parse(BinaryReader reader)
        => new()
        {
            DeviceCert = new(reader.ReadBytesWithLength()),
            SignedThumbprint = reader.ReadBytesWithLength()
        };

    static byte[] CalcThumbprint(X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
    {
        byte[] certData = cert.Export(X509ContentType.Cert);
        byte[] result = new byte[certData.Length + Constants.NonceLength * 2];
        Array.Copy(hostNonce.Value, 0, result, 0, Constants.NonceLength);
        Array.Copy(hostNonce.Value, 0, result, Constants.NonceLength, Constants.NonceLength);
        Array.Copy(certData, 0, result, Constants.NonceLength * 2, certData.Length);
        return result;
    }

    public static DeviceAuthenticationMessage FromCertificate(X509Certificate2 cert, CdpNonce hostNonce, CdpNonce clientNonce)
        => new()
        {
            DeviceCert = cert,
            SignedThumbprint = CalcThumbprint(cert, hostNonce, clientNonce)
        };

    public required X509Certificate2 DeviceCert { get; init; }
    public required byte[] SignedThumbprint { get; init; }

    public bool VerifyThumbprint(CdpNonce hostNonce, CdpNonce clientNonce)
    {
        byte[] expectedThumbprint = CalcThumbprint(DeviceCert, hostNonce, clientNonce);
        return expectedThumbprint.SequenceEqual(DeviceCert.Export(X509ContentType.Cert));
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteWithLength(DeviceCert.Export(X509ContentType.Cert));
        writer.WriteWithLength(SignedThumbprint);
    }
}
