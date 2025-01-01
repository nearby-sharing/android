using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ShortDev.Microsoft.ConnectedDevices;

partial class ConnectedDevicesPlatform
{
    public static X509Certificate2 CreateDeviceCertificate([NotNull] CdpEncryptionParams encryptionParams)
    {
        using var key = ECDsa.Create(encryptionParams.Curve);
        CertificateRequest certRequest = new("CN=Ms-Cdp", key, HashAlgorithmName.SHA256);
        return certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
    }

    public static ILoggerFactory CreateLoggerFactory(string filePattern, LogLevel logLevel = LogLevel.Debug)
        => LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();

            builder.SetMinimumLevel(logLevel);

            builder.AddFile(filePattern, logLevel);
        });
}
