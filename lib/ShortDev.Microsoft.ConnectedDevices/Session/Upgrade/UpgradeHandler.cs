using ShortDev.Microsoft.ConnectedDevices.Internal;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Session.Upgrade;
internal abstract class UpgradeHandler(CdpSession session, EndpointInfo initialEndpoint)
{
    protected readonly CdpSession _session = session;

    // Initial address is always allowed
    protected readonly SynchronizedList<string> _allowedAddresses = [initialEndpoint.Address];
    public bool IsSocketAllowed(CdpSocket socket)
        => _allowedAddresses.Contains(socket.Endpoint.Address);

    public EndpointInfo RemoteEndpoint { get; protected set; } = initialEndpoint;

    public bool IsUpgradeSupported
        => (/* ToDo: header131Value & */ _session.ClientCapabilities & _session.HostCapabilities & PeerCapabilities.UpgradeSupport) != 0;

    public bool TryHandleConnect(CdpSocket socket, ConnectionHeader connectionHeader, ref EndianReader reader)
        => TryHandleConnectInternal(socket, connectionHeader, ref reader);

    protected abstract bool TryHandleConnectInternal(CdpSocket socket, ConnectionHeader connectionHeader, ref EndianReader reader);
}
