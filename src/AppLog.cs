using Android.Content;
using Microsoft.Extensions.Logging;

namespace NearShare;

internal static partial class AppLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Permission result (code {RequestCode}) for permissions {Permissions} with result {GrantResults}")]
    public static partial void RequestPermissionResult(this ILogger logger, int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse share intent: {Intent}")]
    public static partial void FailedToParseShareIntent(this ILogger logger, Exception ex, Intent? intent);
}
