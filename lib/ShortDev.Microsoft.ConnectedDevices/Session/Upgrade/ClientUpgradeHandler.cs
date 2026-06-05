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

    protected override bool TryHandleConnectInternal(CdpSocket socket, ConnectionHeader connectionHeader, ref HeapEndianReader reader)
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
    public async ValueTask<CdpSocket> UpgradeAsync(CdpSocket oldSocket)
    {
        if (Interlocked.CompareExchange(ref _currentUpgrade, new(), null) is not null)
            throw new InvalidOperationException("Only a single upgrade may occur at the same time");

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

            _logger.SendingUpgradeRequest(_currentUpgrade.Id, endpoints);
            _session.SendMessage(
                oldSocket,
                new CommonHeader()
                {
                    Type = MessageType.Connect
                },
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.UpgradeRequest
                },
                new UpgradeRequest()
                {
                    UpgradeId = _currentUpgrade.Id,
                    Endpoints = endpoints
                }
            );

            return await _currentUpgrade;
        }
        finally
        {
            _currentUpgrade = null;
        }
    }

    void HandleUpgradeResponse(CdpSocket oldSocket, ref HeapEndianReader reader)
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
            if (endpoint.TransportType != CdpTransportType.WifiDirect)
                return Task.FromResult<CdpSocket?>(null);

            var metadata = msg.MetaData.FirstOrDefault(x => x.Type == endpoint.TransportType);
            return _session.Platform.TryCreateSocketAsync(endpoint, metadata, UpgradeInstance.Timeout);
        }));

        if (_currentUpgrade == null)
            return;

        if (!_currentUpgrade.TryChooseSocket(tasks.FirstOrDefault(x => x != null)))
            return;

        var wifiDirectTransport = _session.Platform.TryGetTransport<WiFiDirectTransport>();
        SendUpgradFinalization(oldSocket, [
            wifiDirectTransport?.CreateUpgradeFinalization() ?? EndpointMetadata.Tcp
        ]);

        // Cancel after timeout if upgrade has not finished yet
        await Task.Delay(UpgradeInstance.Timeout);

        _currentUpgrade?.TrySetCanceled();
    }

    void SendUpgradFinalization(CdpSocket socket, IReadOnlyList<EndpointMetadata> endpoints)
    {
        _session.SendMessage(
            socket,
            new CommonHeader()
            {
                Type = MessageType.Connect,
            },
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.UpgradeFinalization
            },
            new EndpointMetadataArray(endpoints)
        );
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
        _session.SendMessage(
            _currentUpgrade.NewSocket,
            new CommonHeader()
            {
                Type = MessageType.Connect,
            },
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.TransportRequest
            },
            new UpgradeIdPayload()
            {
                UpgradeId = _currentUpgrade.Id
            }
        );
    }

    void HandleUpgradeFailure(ref HeapEndianReader reader)
    {
        var msg = HResultPayload.Parse(ref reader);

        _currentUpgrade?.TrySetException(
            new Exception($"Transport upgrade failed with HResult {msg.HResult} (hresult: {HResultPayload.HResultToString(msg.HResult)}, errorCode: {HResultPayload.ErrorCodeToString(msg.HResult)})")
        );
    }

    void HandleTransportConfirmation(CdpSocket socket, ref HeapEndianReader reader)
    {
        var msg = UpgradeIdPayload.Parse(ref reader);

        if (_currentUpgrade == null)
            return;

        if (_currentUpgrade.Id != msg.UpgradeId)
            return;

        // Upgrade successful
        RemoteEndpoint = socket.Endpoint;

        // Complete promise
        _currentUpgrade.TrySetResult(socket);
    }

    sealed class UpgradeInstance
    {
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        public Guid Id { get; } = Guid.NewGuid();

        readonly TaskCompletionSource<CdpSocket> _promise = new();
        public bool TrySetCanceled()
            => _promise.TrySetCanceled();

        public bool TrySetResult(CdpSocket socket)
            => _promise.TrySetResult(socket);

        public bool TrySetException(Exception ex)
            => _promise.TrySetException(ex);

        public TaskAwaiter<CdpSocket> GetAwaiter()
            => _promise.Task.GetAwaiter();

        CdpSocket? _newSocket;
        public bool TryChooseSocket(CdpSocket? newSocket)
        {
            if (newSocket is null)
            {
                _promise.TrySetCanceled();
                return false;
            }

            return Interlocked.CompareExchange(ref _newSocket, newSocket, null) is null;
        }

        public CdpSocket? NewSocket
            => _newSocket;
    }
}
