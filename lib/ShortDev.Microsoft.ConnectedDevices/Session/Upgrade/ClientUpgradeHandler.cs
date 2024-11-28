using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Microsoft.ConnectedDevices.Transports.Network;
using ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MessageType = ShortDev.Microsoft.ConnectedDevices.Messages.MessageType;

namespace ShortDev.Microsoft.ConnectedDevices.Session.Upgrade;
internal sealed class ClientUpgradeHandler(CdpSession session, EndpointInfo initialEndpoint) : UpgradeHandler(session, initialEndpoint)
{
    private readonly ILogger _logger = session.Platform.CreateLogger<ClientUpgradeHandler>();

    protected override bool TryHandleConnectInternal(CdpSocket socket, ConnectionHeader connectionHeader, ref EndianReader reader)
    {
        if (!IsSocketAllowed(socket))
            return false;

        switch (connectionHeader.MessageType)
        {
            case ConnectionType.UpgradeResponse:
                HandleUpgradeResponse(socket, ref reader);
                return true;

            case ConnectionType.UpgradeFinalizationResponse:
                HandleUpgradeFinalizationResponse();
                return true;

            case ConnectionType.TransportConfirmation:
                HandleTransportConfirmation(socket, ref reader);
                return true;

            case ConnectionType.UpgradeFailure:
                HandleUpgradeFailure(ref reader);
                return true;
        }
        return false;
    }

    UpgradeInstance? _currentUpgrade;
    public async ValueTask<CdpSocket> RequestUpgradeAsync(CdpSocket oldSocket)
    {
        if (_currentUpgrade != null)
            throw new InvalidOperationException("Only a single upgrade may occur at the same time");

        _currentUpgrade = new();
        try
        {
            List<EndpointMetadata> endpoints = [];

            var networkTransport = _session.Platform.TryGetTransport<NetworkTransport>();
            if (networkTransport is not null)
            {
                endpoints.Add(EndpointMetadata.Tcp);
            }

            var wifiDirectTransport = _session.Platform.TryGetTransport<WiFiDirectTransport>();
            if (wifiDirectTransport is not null)
            {
                endpoints.Add(wifiDirectTransport.CreateUpgradeRequest());
            }

            CommonHeader header = new()
            {
                Type = MessageType.Connect
            };

            EndianWriter writer = new(Endianness.BigEndian);
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.UpgradeRequest
            }.Write(writer);
            new UpgradeRequest()
            {
                UpgradeId = _currentUpgrade.Id,
                Endpoints = endpoints
            }.Write(writer);

            _logger.SendingUpgradeRequest(_currentUpgrade.Id, endpoints);
            _session.SendMessage(oldSocket, header, writer);

            return await _currentUpgrade.Promise.Task;
        }
        finally
        {
            _currentUpgrade = null;
        }
    }

    void HandleUpgradeResponse(CdpSocket oldSocket, ref EndianReader reader)
    {
        if (_currentUpgrade == null)
            return;

        var msg = UpgradeResponse.Parse(ref reader);
        _logger.UpgradeResponse(_currentUpgrade.Id, msg.Endpoints);

        HandleUpgradeResponse(oldSocket, msg);
    }

    async void HandleUpgradeResponse(CdpSocket oldSocket, UpgradeResponse msg)
    {
        var tasks = await Task.WhenAll(msg.Endpoints.Select(endpoint =>
        {
            var metadata = msg.MetaData.FirstOrDefault(x => x.Type == endpoint.TransportType);
            return _session.Platform.TryCreateSocketAsync(endpoint, metadata, UpgradeInstance.Timeout);
        }));

        if (_currentUpgrade == null)
            return;

        _currentUpgrade.NewSocket = tasks.FirstOrDefault(x => x != null);
        if (_currentUpgrade.NewSocket == null)
        {
            _currentUpgrade.Promise.TrySetCanceled();
            return;
        }

        var wifiDirectTransport = _session.Platform.TryGetTransport<WiFiDirectTransport>();
        SendUpgradFinalization(oldSocket, [
            wifiDirectTransport?.CreateUpgradeFinalization() ?? EndpointMetadata.Tcp
        ]);

        // Cancel after timeout if upgrade has not finished yet
        await Task.Delay(UpgradeInstance.Timeout);

        _currentUpgrade?.Promise.TrySetCanceled();
    }

    void SendUpgradFinalization(CdpSocket socket, IReadOnlyList<EndpointMetadata> endpoints)
    {
        EndianWriter writer = new(Endianness.BigEndian);
        new ConnectionHeader()
        {
            ConnectionMode = ConnectionMode.Proximal,
            MessageType = ConnectionType.UpgradeFinalization
        }.Write(writer);
        EndpointMetadata.WriteArray(writer, endpoints);

        _session.SendMessage(socket, new()
        {
            Type = MessageType.Connect,
        }, writer);
    }

    void HandleUpgradeFinalizationResponse()
    {
        // Upgrade has been acknowledged

        if (_currentUpgrade == null)
            return;

        Debug.Assert(_currentUpgrade.NewSocket != null);

        // Allow the new address
        _allowedAddresses.Add(_currentUpgrade.NewSocket.Endpoint.Address);

        // Request transport permission for new socket
        EndianWriter writer = new(Endianness.BigEndian);
        new ConnectionHeader()
        {
            ConnectionMode = ConnectionMode.Proximal,
            MessageType = ConnectionType.TransportRequest
        }.Write(writer);
        new UpgradeIdPayload()
        {
            UpgradeId = _currentUpgrade.Id
        }.Write(writer);

        _session.SendMessage(_currentUpgrade.NewSocket, new()
        {
            Type = MessageType.Connect,
        }, writer);
    }

    void HandleUpgradeFailure(ref EndianReader reader)
    {
        var msg = HResultPayload.Parse(ref reader);

        _currentUpgrade?.Promise.TrySetException(
            new Exception($"Transport upgrade failed with HResult {msg.HResult} (hresult: {HResultPayload.HResultToString(msg.HResult)}, errorCode: {HResultPayload.ErrorCodeToString(msg.HResult)})")
        );
    }

    void HandleTransportConfirmation(CdpSocket socket, ref EndianReader reader)
    {
        var msg = UpgradeIdPayload.Parse(ref reader);

        if (_currentUpgrade == null)
            return;

        if (_currentUpgrade.Id != msg.UpgradeId)
            return;

        // Upgrade successful
        RemoteEndpoint = socket.Endpoint;

        // Complete promise
        _currentUpgrade.Promise.TrySetResult(socket);
    }

    sealed class UpgradeInstance
    {
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        public Guid Id { get; } = Guid.NewGuid();
        public TaskCompletionSource<CdpSocket> Promise { get; } = new();

        public TaskAwaiter<CdpSocket> GetAwaiter()
            => Promise.Task.GetAwaiter();

        public CdpSocket? NewSocket { get; set; }
    }
}
