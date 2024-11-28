using Android.Net;
using Android.Net.Wifi.P2p;
using Microsoft.Extensions.Logging;

namespace NearShare.Droid;

internal static partial class AppLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Permission result (code {RequestCode}) for permissions {Permissions} with result {GrantResults}")]
    public static partial void RequestPermissionResult(this ILogger logger, int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults);

    [LoggerMessage(Level = LogLevel.Information, Message = "Respond with {response} to {requestType} request by '{macAddress}'")]
    public static partial void WiFiDirectApproveResult(this ILogger logger, ConnectionRequestType response, ExternalApproverRequestType requestType, MacAddress macAddress);
}
