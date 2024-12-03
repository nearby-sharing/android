using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Transports.Network;
using System.Security.Cryptography;
using System.Text;
using static ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect.MetaDataWriter;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
public sealed class WiFiDirectTransport(IWiFiDirectHandler handler, NetworkTransport networkTransport) : ICdpTransport
{
    readonly IWiFiDirectHandler _handler = handler;
    readonly NetworkTransport _networkTransport = networkTransport;

    public CdpTransportType TransportType { get; } = CdpTransportType.WifiDirect;

    public async Task<CdpSocket> ConnectAsync(EndpointInfo endpoint)
        => await ConnectAsync(endpoint, null);

    public async Task<CdpSocket> ConnectAsync(EndpointInfo endpoint, EndpointMetadata? metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        ParseHostResponse(metadata.Data, out var address, out var ssid, out var sharedKey);

        var hostIp = await _handler.ConnectAsync(endpoint.Address, ssid, sharedKey);
        return await _networkTransport.ConnectAsync(new EndpointInfo(CdpTransportType.Tcp, hostIp.ToString(), "5160"));
    }

    // ToDo: Cannot listen
    public Task Listen(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public event DeviceConnectedEventHandler? DeviceConnected;
    public EndpointInfo GetEndpoint()
        => new(CdpTransportType.WifiDirect, _handler.MacAddress.ToStringFormatted(), "");

    #region Upgrade
    internal EndpointMetadata CreateUpgradeRequest()
    {
        const GroupRole rolePreference = GroupRole.Client;

        EndianWriter writer = new(Endianness.BigEndian);
        WriteHeader(ref writer, MessageType.ClientAvailableForUpgrade, _handler.MacAddress);
        WriteField(ref writer, MessageValueType.RolePreference, [(byte)rolePreference]);
        return new(CdpTransportType.WifiDirect, writer.Buffer.ToArray());
    }

    internal EndpointMetadata CreateUpgradeResponse()
    {
        const GroupRole roleDecision = GroupRole.GroupOwner;

        byte[] sharedKey = new byte[32];
        RandomNumberGenerator.Fill(sharedKey);

        // ToDo: This should be configurable
        var ssid = "DIRECT-CDP";

        _ = _handler.CreateGroupAutonomous(ssid, sharedKey);

        EndianWriter writer = new(Endianness.BigEndian);
        WriteHeader(ref writer, MessageType.HostGetUpgradeEndpoints, _handler.MacAddress);
        WriteField(ref writer, MessageValueType.RoleDecision, [(byte)roleDecision]);
        WriteField(ref writer, MessageValueType.GOPreSharedKey, sharedKey);
        WriteField(ref writer, MessageValueType.GOSSID, Encoding.UTF8.GetBytes(ssid));
        return new(CdpTransportType.WifiDirect, writer.Buffer.ToArray());
    }

    internal EndpointMetadata CreateUpgradeFinalization()
    {
        EndianWriter writer = new(Endianness.BigEndian);
        WriteHeader(ref writer, MessageType.ClientFinalizeUpgrade, _handler.MacAddress);
        return new(CdpTransportType.WifiDirect, writer.Buffer.ToArray());
    }
    #endregion

    public void Dispose()
        => _handler.Dispose();
}
