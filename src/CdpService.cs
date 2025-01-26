using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;
using NearShare.Utils;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using OperationCanceledException = System.OperationCanceledException;

namespace NearShare;

[Service(Exported = true)]
public sealed class CdpService : Service
{
    #region Connection
    public override IBinder? OnBind(Intent? intent) => null;

    [return: GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        => StartCommandResult.Sticky;

    static CdpService? _instance;
    public static async ValueTask<CdpService> EnsureRunning(Context context, CancellationToken cancellationToken = default)
    {
        if (_instance != null)
            return _instance;

        context.StartService(new Intent(context, typeof(CdpService)));

        while (_instance == null)
            await Task.Delay(10, cancellationToken);

        return _instance ?? throw new InvalidOperationException($"Could not get instance of {nameof(CdpService)}");
    }
    #endregion

    readonly CancellationTokenSource _discoveryCancellation = new();
    ConnectedDevicesPlatform? _cdp;
    ILogger? _logger;
    NotificationManager _notifications = null!;
    public override void OnCreate()
    {
        _notifications = (NotificationManager?)GetSystemService(NotificationService) ?? throw new NullReferenceException("Could not get notifcation service");

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            _notifications.CreateNotificationChannel(new(SendChannelId, "Send to nearby device", NotificationImportance.Min));
        }

        var loggerFactory = CdpUtils.CreateLoggerFactory(this);
        _logger = loggerFactory.CreateLogger<CdpService>();

        _cdp = CdpUtils.Create(this, loggerFactory);

        _instance = this;
    }

    public ConnectedDevicesPlatform Platform
        => _cdp ?? throw new InvalidOperationException("Service not initialized");

    public override void OnDestroy()
    {
        _instance = null;

        if (_cdp != null)
        {
            _cdp.Dispose();
            _cdp = null;
        }
    }

    const string SendChannelId = "de.shortdev.nearshare.send";

    public async void ShowTransferNotification(CdpDevice remoteSystem, Task task, Progress<NearShareProgress> progress)
    {
        var id = progress.GetHashCode();
        var notification = new NotificationCompat.Builder(this, SendChannelId)
            .SetOngoing(true)
            .SetSmallIcon(Resource.Drawable.ic_fluent_share_24_selector)
            .SetContentTitle($"Sharing with {remoteSystem.Name}")
            // .AddAction(Resource.Drawable.ic_fluent_dismiss_24_selector, "Cancel", PendingIntent.GetActivity())
            .SetProgress(0, 0, indeterminate: true);

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            notification.SetChannelId(SendChannelId);
        }

        _notifications.Notify(id, notification.Build());
        progress.ProgressChanged += OnProgressChanged;

        try
        {
            await task;

            notification.SetOngoing(false);
            _notifications.Notify(id, notification.Build());
        }
        catch (OperationCanceledException)
        {
            _notifications.Cancel(id);
        }
        catch (Exception ex)
        {
            notification.SetContentText(ex.Message);

            notification.SetOngoing(false);
            _notifications.Notify(id, notification.Build());
        }

        void OnProgressChanged(object? sender, NearShareProgress e)
        {
            if (e.TransferedBytes >= e.TotalBytes)
                progress.ProgressChanged -= OnProgressChanged;

            var percent = (int)(e.TransferedBytes * 100 / e.TotalBytes);
            notification.SetProgress(max: 100, percent, indeterminate: false);
            _notifications.Notify(id, notification.Build());
        }
    }
}
