using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System;
using System.Collections.Generic;

namespace ShortDev.Microsoft.ConnectedDevices;

internal static partial class CdpLog
{
    [LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "Advertising started")]
    public static partial void AdvertisingStarted(this ILogger logger);

    [LoggerMessage(EventId = 102, Level = LogLevel.Information, Message = "Advertising stopped")]
    public static partial void AdvertisingStopped(this ILogger logger);


    [LoggerMessage(EventId = 103, Level = LogLevel.Information, Message = "Listening started")]
    public static partial void ListeningStarted(this ILogger logger);

    [LoggerMessage(EventId = 104, Level = LogLevel.Information, Message = "Listening stopped")]
    public static partial void ListeningStopped(this ILogger logger);


    [LoggerMessage(EventId = 105, Level = LogLevel.Information, Message = "Device {DeviceName} connected with endpoint {Endpoint}")]
    public static partial void DeviceConnected(this ILogger logger, string deviceName, EndpointInfo endpoint);



    [LoggerMessage(EventId = 201, Level = LogLevel.Error, Message = "Exception in session {SessionId:X}")]
    public static partial void ExceptionInSession(this ILogger logger, Exception ex, ulong sessionId);

    [LoggerMessage(EventId = 202, Level = LogLevel.Error, Message = "Exception in receive loop for transport {TransportType}")]
    public static partial void ExceptionInReceiveLoop(this ILogger logger, Exception ex, CdpTransportType transportType);

    [LoggerMessage(EventId = 203, Level = LogLevel.Debug, Message = "Received connect message {ConectMessageType} from session {SessionId:X} via {TransportType}")]
    public static partial void ReceivedConnectMessage(this ILogger logger, ConnectionType conectMessageType, ulong sessionId, CdpTransportType transportType);

    [LoggerMessage(EventId = 204, Level = LogLevel.Debug, Message = "Received control message {ControlMessageType} from session {SessionId:X} via {TransportType}")]
    public static partial void ReceivedControlMessage(this ILogger logger, ControlMessageType controlMessageType, ulong sessionId, CdpTransportType transportType);


    [LoggerMessage(EventId = 301, Level = LogLevel.Debug, Message = "Upgrade request {UpgradeId} to {UpgradeTypes}")]
    public static partial void UpgradeRequest(this ILogger logger, Guid upgradeId, IEnumerable<CdpTransportType> upgradeTypes);

    [LoggerMessage(EventId = 302, Level = LogLevel.Debug, Message = "Finalizing upgrade to {UpgradeTypes}")]
    public static partial void UpgradeFinalization(this ILogger logger, IEnumerable<CdpTransportType> upgradeTypes);

    [LoggerMessage(EventId = 303, Level = LogLevel.Information, Message = "Transport upgrade {UpgradeId} {UpgradeStatus}")]
    public static partial void UpgradeTransportRequest(this ILogger logger, Guid upgradeId, string upgradeStatus);

    [LoggerMessage(EventId = 304, Level = LogLevel.Warning, Message = "Upgrade failed")]
    public static partial void UpgradeFailed(this ILogger logger, Exception ex);
}
