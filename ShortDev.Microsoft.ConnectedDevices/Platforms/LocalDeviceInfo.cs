using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms;

public sealed class LocalDeviceInfo
{
    public required DeviceType Type { get; init; }
    public required string Name { get; init; }

    public required X509Certificate2 DeviceCertificate { get; init; }

    public required string OemManufacturerName { get; init; }
    public required string OemModelName { get; init; }

    static readonly HashAlgorithm _hashAlogrithm = SHA512.Create();
    public byte[] GetDeduplicationHint()
    {
        var input = $"{Name}{OemManufacturerName}{OemModelName}{(byte)Type.GetPlatformType()}";
        var inputBuffer = Encoding.UTF8.GetBytes(input);

        Span<byte> hashBuffer = stackalloc byte[64];
        Debug.Assert(_hashAlogrithm.TryComputeHash(inputBuffer, hashBuffer, out var bytesWritten));
        Debug.Assert(bytesWritten == hashBuffer.Length);

        var base64Str = Convert.ToBase64String(hashBuffer);
        return Encoding.ASCII.GetBytes(base64Str);
    }

    public CdpDeviceInfo ToCdpDeviceInfo(IReadOnlyList<EndpointInfo> endpoints)
    {
        var deduplicationHint = GetDeduplicationHint();
        return new()
        {
            Type = Type,
            Name = Name,
            ConnectionModes = Messages.Connection.ConnectionMode.Proximal,
            DeviceId = deduplicationHint, // ToDo: Wrong
            DeduplicationHint = deduplicationHint,
            Endpoints = endpoints
        };
    }
}
