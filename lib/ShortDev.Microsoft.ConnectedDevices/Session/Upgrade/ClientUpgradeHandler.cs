using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

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
            SendUpgradeRequest(oldSocket, _currentUpgrade.Id);
            return await _currentUpgrade.Promise.Task;
        }
        finally
        {
            _currentUpgrade = null;
        }

        void SendUpgradeRequest(CdpSocket socket, Guid upgradeId)
        {
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

            EndpointMetadata[] endpoints = [
                EndpointMetadata.Tcp,
                WiFiDirectMetaData.GetUpgradeRequest(new(RandomNumberGenerator.GetBytes(6)))
            ];
            _logger.SendingUpgradeRequest(_currentUpgrade.Id, endpoints);
            new UpgradeRequest()
            {
                UpgradeId = upgradeId,
                Endpoints = endpoints
            }.Write(writer);

            _session.SendMessage(socket, header, writer);
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

                return await _session.Platform.TryCreateSocketAsync(endpoint, UpgradeInstance.Timeout);
            }));

            if (_currentUpgrade == null)
                return;

            _currentUpgrade.NewSocket = tasks.FirstOrDefault(x => x != null);
            if (_currentUpgrade.NewSocket == null)
            {
                _currentUpgrade.Promise.TrySetCanceled();
                return;
            }

            SendUpgradFinalization(oldSocket);

            // Cancel after timeout if upgrade has not finished yet
            await Task.Delay(UpgradeInstance.Timeout);

            _currentUpgrade?.Promise.TrySetCanceled();
        }
    }

    void SendUpgradFinalization(CdpSocket socket)
    {
        EndianWriter writer = new(Endianness.BigEndian);
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
