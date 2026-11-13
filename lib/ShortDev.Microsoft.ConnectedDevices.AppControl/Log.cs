using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.AppControl.Messages;

namespace ShortDev.Microsoft.ConnectedDevices.AppControl;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Unexpected app control message of type '{messageType}'")]
    public static partial void UnexpectedMessage(this ILogger<AppControlApp> logger, AppControlType messageType);
}
