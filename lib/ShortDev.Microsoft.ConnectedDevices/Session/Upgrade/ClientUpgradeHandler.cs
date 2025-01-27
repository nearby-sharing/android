using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

    static readonly IReadOnlyList<EndpointMetadata> UpgradeEndpoints = [EndpointMetadata.Tcp];

    UpgradeInstance? _currentUpgrade;
    public async ValueTask<CdpSocket> UpgradeAsync(CdpSocket oldSocket)
    {
        if (Interlocked.CompareExchange(ref _currentUpgrade, new(), null) is not null)
            throw new InvalidOperationException("Only a single upgrade may occur at the same time");

        try
        {
            _logger.SendingUpgradeRequest(_currentUpgrade.Id, UpgradeEndpoints);

            CommonHeader header = new()
            {
                Type = MessageType.Connect
            };

            using (var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool))
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.UpgradeRequest
                }.Write(writer);

                new UpgradeRequest()
                {
                    UpgradeId = _currentUpgrade.Id,
                    Endpoints = UpgradeEndpoints
                }.Write(writer);

                _session.SendMessage(oldSocket, header, writer);
            }

            return await _currentUpgrade;
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

        FindNewEndpoint();

        async void FindNewEndpoint()
        {
            var tasks = await Task.WhenAll(msg.Endpoints.Select(async endpoint =>
            {
                if (endpoint.TransportType != CdpTransportType.Tcp)
                    return null; // ToDo: Only Tcp upgrade supported by Windows ?!

                if (!int.TryParse(endpoint.Service, out var port))
                    return null;

                return await _session.Platform.TryCreateSocketAsync(endpoint, UpgradeInstance.Timeout).ConfigureAwait(false);
            })).ConfigureAwait(false);

            if (_currentUpgrade == null)
                return;

            if (!_currentUpgrade.TryChooseSocket(tasks.FirstOrDefault(x => x != null)))
                return;

            SendUpgradFinalization(oldSocket);

            // Cancel after timeout if upgrade has not finished yet
            await Task.Delay(UpgradeInstance.Timeout).ConfigureAwait(false);

            _currentUpgrade?.TrySetCanceled();
        }
    }

    void SendUpgradFinalization(CdpSocket socket)
    {
        using var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
        new ConnectionHeader()
        {
            ConnectionMode = ConnectionMode.Proximal,
            MessageType = ConnectionType.UpgradeFinalization
        }.Write(writer);
        EndpointMetadata.WriteArray(writer,
        [
            EndpointMetadata.Tcp
        ]);

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
        using var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
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

        _currentUpgrade?.TrySetException(
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
        _currentUpgrade.TrySetResult(socket);
    }

    sealed class UpgradeInstance
    {
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

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
