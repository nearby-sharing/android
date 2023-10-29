using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System;

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



    [LoggerMessage(EventId = 201, Level = LogLevel.Warning, Message = "Exception in session {SessionId:X}")]
    public static partial void ExceptionInSession(this ILogger logger, Exception ex, ulong sessionId);

    [LoggerMessage(EventId = 202, Level = LogLevel.Warning, Message = "Exception in receive loop for transport {TransportType}")]
    public static partial void ExceptionInReceiveLoop(this ILogger logger, Exception ex, CdpTransportType transportType);
}
