using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

/// <summary>
/// This message transports the details of the upgrade.
/// </summary>
public sealed class TransportRequest : ICdpPayload<TransportRequest>
{
    public required Guid UpgradeId { get; init; }

    public static TransportRequest Parse(BinaryReader reader)
        => new()
        {
            UpgradeId = new(reader.ReadBytes(16))
        };

    public void Write(BinaryWriter writer)
    {
        writer.Write(UpgradeId.ToByteArray());
    }
}
