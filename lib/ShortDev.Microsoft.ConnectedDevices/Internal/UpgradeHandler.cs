using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ShortDev.Microsoft.ConnectedDevices.Internal;

internal sealed class UpgradeHandler
{
    readonly ILogger<UpgradeHandler> _logger;
    readonly CdpSession _session;
    public UpgradeHandler(CdpSession session, CdpDevice initalDevice)
    {
        _session = session;
        _logger = session.Platform.CreateLogger<UpgradeHandler>();

        // Initial address is always allowed
        _allowedAddresses.Add(initalDevice.Endpoint.Address);
    }

    #region Policies
    public bool IsUpgradeSupported
        => (/* ToDo: header131Value & */ _session.ClientCapabilities & _session.HostCapabilities & PeerCapabilities.UpgradeSupport) != 0;

    readonly ConcurrentList<string> _allowedAddresses = new();
    public bool IsSocketAllowed(CdpSocket socket)
        => _allowedAddresses.Contains(socket.RemoteDevice.Endpoint.Address);
    #endregion

    public bool TryHandleConnect(CdpSocket socket, ConnectionHeader connectionHeader, ref EndianReader reader)
    {
        // This part needs to be always accessible!
        // This is used to validate
        if (connectionHeader.MessageType == ConnectionType.TransportRequest)
        {
            _session.ThrowIfWrongMode(true);
            HandleTransportRequest(socket, ref reader);
            return true;
        }

        // If invalid socket, return false and let CdpSession.HandleConnect throw
        if (!IsSocketAllowed(socket))
            return false;

        switch (connectionHeader.MessageType)
        {
            // Host
            case ConnectionType.UpgradeRequest:
                _session.ThrowIfWrongMode(true);
                HandleUpgradeRequest(socket, ref reader);
                return true;
            case ConnectionType.UpgradeFinalization:
                _session.ThrowIfWrongMode(true);
                HandleUpgradeFinalization(socket, ref reader);
                return true;

            case ConnectionType.UpgradeFailure:
                HandleUpgradeFailure(ref reader);
                return true;

            // Client
            case ConnectionType.UpgradeResponse:
                _session.ThrowIfWrongMode(false);
                HandleUpgradeResponse(socket, ref reader);
                return true;
            case ConnectionType.UpgradeFinalizationResponse:
                _session.ThrowIfWrongMode(false);
                HandleUpgradeFinalizationResponse();
                return true;
            case ConnectionType.TransportConfirmation:
                _session.ThrowIfWrongMode(false);
                HandleTransportConfirmation(socket, ref reader);
                return true;
        }
        return false;
    }

    #region Host
    readonly ConcurrentList<Guid> _upgradeIds = new();
    void HandleTransportRequest(CdpSocket socket, ref EndianReader reader)
    {
        var msg = UpgradeIdPayload.Parse(ref reader);

        // Sometimes the device sends multiple transport requests
        // If we know it already then let it pass
        bool allowed = IsSocketAllowed(socket);
        if (!allowed && _upgradeIds.Contains(msg.UpgradeId))
        {
            // No we have confirmed that this address belongs to the same device (different transport)
            _allowedAddresses.Add(socket.RemoteDevice.Endpoint.Address);
            _upgradeIds.Remove(msg.UpgradeId);

            allowed = true;
        }

        _logger.UpgradeTransportRequest(
            msg.UpgradeId,
            allowed ? "succeeded" : "failed"
        );

        CommonHeader header = new()
        {
            Type = MessageType.Connect
        };

        _session.SendMessage(socket, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = allowed ? ConnectionType.TransportConfirmation : ConnectionType.UpgradeFailure
            }.Write(writer);
            msg.Write(writer);
        });
    }

    void HandleUpgradeRequest(CdpSocket socket, ref EndianReader reader)
    {
        var msg = UpgradeRequest.Parse(ref reader);
        _logger.UpgradeRequest(
            msg.UpgradeId,
            msg.Endpoints.Select((x) => x.Type)
        );

        CommonHeader header = new()
        {
            Type = MessageType.Connect
        };

        var localIp = _session.Platform.TryGetTransport<NetworkTransport>()?.Handler.TryGetLocalIp();
        if (localIp == null)
        {
            _session.SendMessage(socket, header, (writer) =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.UpgradeFailure
                }.Write(writer);
                new HResultPayload()
                {
                    HResult = -1
                }.Write(writer);
            });

            return;
        }

        _upgradeIds.Add(msg.UpgradeId);

        _session.SendMessage(socket, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.UpgradeResponse
            }.Write(writer);
            new UpgradeResponse()
            {
                Endpoints =
                [
                    EndpointInfo.FromTcp(localIp)
                ],
                MetaData =
                [
                    EndpointMetadata.Tcp
                ]
            }.Write(writer);
        });
    }

    void HandleUpgradeFinalization(CdpSocket socket, ref EndianReader reader)
    {
        var msg = EndpointMetadata.ParseArray(ref reader);
        _logger.UpgradeFinalization(
            msg.Select((x) => x.Type)
        );

        CommonHeader header = new()
        {
            Type = MessageType.Connect
        };

        _session.SendMessage(socket, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.UpgradeFinalizationResponse
            }.Write(writer);
        });
    }
    #endregion

    void HandleUpgradeFailure(ref EndianReader reader)
    {
        var msg = HResultPayload.Parse(ref reader);

        _currentUpgrade?.Promise.TrySetException(
            new Exception($"Transport upgrade failed with HResult {msg.HResult} (hresult: {HResultPayload.HResultToString(msg.HResult)}, errorCode: {HResultPayload.ErrorCodeToString(msg.HResult)})")
        );
    }

    #region Client
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

            _session.SendMessage(socket, header, writer =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.UpgradeRequest
                }.Write(writer);

                new UpgradeRequest()
                {
                    UpgradeId = upgradeId,
                    Endpoints =
                    [
                        EndpointMetadata.Tcp
                    ]
                }.Write(writer);
            });
        }
    }

    void HandleUpgradeResponse(CdpSocket oldSocket, ref EndianReader reader)
    {
        if (_currentUpgrade == null)
            return;

        var msg = UpgradeResponse.Parse(ref reader);
        FindNewEndpoint();

        async void FindNewEndpoint()
        {
            var tasks = await Task.WhenAll(msg.Endpoints.Select(async endpoint =>
            {
                if (endpoint.TransportType != CdpTransportType.Tcp)
                    return null; // ToDo: Only Tcp upgrade supported by Windows ?!

                if (!int.TryParse(endpoint.Service, out var port))
                    return null;

                return await _session.Platform.TryCreateSocketAsync(_session.Device.WithEndpoint(endpoint), UpgradeInstance.Timeout);
            }));

            if (_currentUpgrade == null)
                return;

            _currentUpgrade.NewSocket = tasks.FirstOrDefault(x => x != null);
            if (_currentUpgrade.NewSocket == null)
            {
                _currentUpgrade.Promise.TrySetCanceled();
                return;
            }

            _session.SendMessage(oldSocket, new()
            {
                Type = MessageType.Connect,
            }, writer =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.UpgradeFinalization
                }.Write(writer);
                EndpointMetadata.WriteArray(writer,
                [
                    EndpointMetadata.Tcp
                ]);
            });

            // Cancel after timeout if upgrade has not finished yet
            await Task.Delay(UpgradeInstance.Timeout);

            _currentUpgrade?.Promise.TrySetCanceled();
        }
    }

    void HandleUpgradeFinalizationResponse()
    {
        // Upgrade has been acknowledged

        if (_currentUpgrade == null)
            return;

        Debug.Assert(_currentUpgrade.NewSocket != null);

        // Allow the new address
        _allowedAddresses.Add(_currentUpgrade.NewSocket.RemoteDevice.Endpoint.Address);

        // Request transport permission for new socket
        _session.SendMessage(_currentUpgrade.NewSocket, new()
        {
            Type = MessageType.Connect,
        }, writer =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.TransportRequest
            }.Write(writer);
            new UpgradeIdPayload()
            {
                UpgradeId = _currentUpgrade.Id
            }.Write(writer);
        });
    }

    void HandleTransportConfirmation(CdpSocket socket, ref EndianReader reader)
    {
        var msg = UpgradeIdPayload.Parse(ref reader);

        if (_currentUpgrade == null)
            return;

        if (_currentUpgrade.Id != msg.UpgradeId)
            return;

        // Upgrade successful
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
    #endregion
}
