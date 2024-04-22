namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

/// <summary>
/// This message transports the details of the upgrade. <br/>
/// (See <see cref="ConnectionType.TransportRequest"/> and <see cref="ConnectionType.TransportConfirmation"/>)
/// </summary>
public sealed class UpgradeIdPayload : ICdpPayload<UpgradeIdPayload>
{
    public required Guid UpgradeId { get; init; }

    public static UpgradeIdPayload Parse(ref EndianReader reader)
        => new()
        {
            UpgradeId = reader.ReadGuid()
        };

    public void Write(EndianWriter writer)
    {
        writer.Write(UpgradeId.ToByteArray());
    }
}
