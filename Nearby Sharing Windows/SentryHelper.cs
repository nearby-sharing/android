using Sentry;

namespace Nearby_Sharing_Windows;

internal static class SentryHelper
{
    public static void EnsureInitialized()
    {
        if (SentrySdk.IsEnabled)
            return;

        SentrySdk.Init(options =>
        {
            options.Dsn = "https://47f9f6c3642149a5af942e8484e64fe1@o646413.ingest.sentry.io/6437134";
            options.TracesSampleRate = 0.7;
        });
    }
}
