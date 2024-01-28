using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices;

internal static partial class CdpLog
{
    #region Advertising
    [LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "Advertising started")]
    public static partial void AdvertisingStarted(this ILogger logger);

    [LoggerMessage(EventId = 102, Level = LogLevel.Information, Message = "Advertising stopped")]
    public static partial void AdvertisingStopped(this ILogger logger);

    [LoggerMessage(EventId = 106, Level = LogLevel.Information, Message = "Error while advertising")]
    public static partial void AdvertisingError(this ILogger logger, Exception ex);
    #endregion

    #region Listening
    [LoggerMessage(EventId = 103, Level = LogLevel.Information, Message = "Listening started")]
    public static partial void ListeningStarted(this ILogger logger);

    [LoggerMessage(EventId = 104, Level = LogLevel.Information, Message = "Listening stopped")]
    public static partial void ListeningStopped(this ILogger logger);

    [LoggerMessage(EventId = 107, Level = LogLevel.Information, Message = "Error while listening")]
    public static partial void ListeningError(this ILogger logger, Exception ex);
    #endregion

    #region Discovery
    [LoggerMessage(EventId = 108, Level = LogLevel.Information, Message = "Discovery started on {TransportTypes}")]
    public static partial void DiscoveryStarted(this ILogger logger, IEnumerable<CdpTransportType> transportTypes);

    [LoggerMessage(EventId = 109, Level = LogLevel.Information, Message = "Discovery stopped")]
    public static partial void DiscoveryStopped(this ILogger logger);

    [LoggerMessage(EventId = 110, Level = LogLevel.Information, Message = "Error during discovery")]
    public static partial void DiscoveryError(this ILogger logger, Exception ex);
    #endregion

    [LoggerMessage(EventId = 105, Level = LogLevel.Information, Message = "New socket from endpoint {Endpoint}")]
    public static partial void NewSocket(this ILogger logger, EndpointInfo endpoint);


    [LoggerMessage(EventId = 201, Level = LogLevel.Error, Message = "Exception in session {SessionId:X}")]
    public static partial void ExceptionInSession(this ILogger logger, Exception ex, ulong sessionId);

    [LoggerMessage(EventId = 202, Level = LogLevel.Error, Message = "Exception in receive loop for transport {TransportType}")]
    public static partial void ExceptionInReceiveLoop(this ILogger logger, Exception ex, CdpTransportType transportType);

    [LoggerMessage(EventId = 203, Level = LogLevel.Debug, Message = "Received connect message {ConectMessageType} from session {SessionId:X} via {TransportType}")]
    public static partial void ReceivedConnectMessage(this ILogger logger, ConnectionType conectMessageType, ulong sessionId, CdpTransportType transportType);

    [LoggerMessage(EventId = 204, Level = LogLevel.Debug, Message = "Received control message {ControlMessageType} from session {SessionId:X} via {TransportType}")]
    public static partial void ReceivedControlMessage(this ILogger logger, ControlMessageType controlMessageType, ulong sessionId, CdpTransportType transportType);

    [LoggerMessage(EventId = 205, Level = LogLevel.Debug, Message = "Received device info: {DeviceInfo}")]
    public static partial void ReceivedDeviceInfo(this ILogger logger, CdpDeviceInfo deviceInfo);


    [LoggerMessage(EventId = 301, Level = LogLevel.Debug, Message = "Upgrade request {UpgradeId} to {UpgradeTypes}")]
    public static partial void UpgradeRequest(this ILogger logger, Guid upgradeId, IEnumerable<CdpTransportType> upgradeTypes);

    [LoggerMessage(EventId = 302, Level = LogLevel.Debug, Message = "Finalizing upgrade to {UpgradeTypes}")]
    public static partial void UpgradeFinalization(this ILogger logger, IEnumerable<CdpTransportType> upgradeTypes);

    [LoggerMessage(EventId = 303, Level = LogLevel.Information, Message = "Transport upgrade {UpgradeId} {UpgradeStatus}")]
    public static partial void UpgradeTransportRequest(this ILogger logger, Guid upgradeId, string upgradeStatus);

    [LoggerMessage(EventId = 304, Level = LogLevel.Warning, Message = "Upgrade failed")]
    public static partial void UpgradeFailed(this ILogger logger, Exception ex);

    [LoggerMessage(EventId = 305, Level = LogLevel.Debug, Message = "Sending upgrade request {UpgradeId} to {UpgradeTypes}")]
    public static partial void SendingUpgradeRequest(this ILogger logger, Guid upgradeId, IEnumerable<EndpointMetadata> upgradeTypes);

    [LoggerMessage(EventId = 306, Level = LogLevel.Debug, Message = "Upgrade response {UpgradeId} to {Endpoints}")]
    public static partial void UpgradeResponse(this ILogger logger, Guid upgradeId, IEnumerable<EndpointInfo> endpoints);
}
