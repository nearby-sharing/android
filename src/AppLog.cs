using Microsoft.Extensions.Logging;

namespace NearShare.Droid;

internal static partial class AppLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Permission result (code {RequestCode}) for permissions {Permissions} with result {GrantResults}")]
    public static partial void RequestPermissionResult(this ILogger logger, int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults);
}
