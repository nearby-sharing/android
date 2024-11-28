using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Transports.Network;
using System.Net.NetworkInformation;
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
        return await _networkTransport.ConnectAsync(EndpointInfo.FromTcp(hostIp));
    }

    public Task Listen(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

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

        Span<byte> sharedKey = stackalloc byte[32];
        RandomNumberGenerator.Fill(sharedKey);

        var ssid = "DIRECT-CDP";
        var passphrase = Convert.ToBase64String(sharedKey);

        _ = _handler.CreateGroupAutonomous(ssid, passphrase);

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
