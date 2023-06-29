using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;
using Nearby_Sharing_Windows.Settings;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Net.NetworkInformation;

namespace Nearby_Sharing_Windows.Service;

[Service(Exported = true)]
public sealed class CdpService : Android.App.Service, INearSharePlatformHandler
{
    #region Connection
    public override IBinder? OnBind(Intent? intent)
        => new CdpServiceBinder(this);

    [return: GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        => StartCommandResult.Sticky;

    static TaskCompletionSource? _promise;
    static CdpService? _instance;
    public static async ValueTask<CdpService> EnsureRunning(Context context)
    {
        if (_instance != null)
            return _instance;

        _promise = new();

        context.StartService(new Intent(context, typeof(CdpService)));

        await _promise.Task;
        _promise = null;

        return _instance ?? throw new InvalidOperationException($"Could not get instance of {nameof(CdpService)}");
    }
    #endregion

    CancellationTokenSource? _sendCancellationTokenSource;
    CancellationTokenSource? _receiveCancellationTokenSource;
    ConnectedDevicesPlatform? _cdp;
    ILogger? _logger;
    public override void OnCreate()
    {
        var loggerFactory = ConnectedDevicesPlatform.CreateLoggerFactory(msg => System.Diagnostics.Debug.Print(msg));
        _logger = loggerFactory.CreateLogger<CdpService>();

        var service = (BluetoothManager)GetSystemService(BluetoothService)!;
        var btAdapter = service.Adapter ?? throw new NullReferenceException("Could not get bt adapter");

        _sendCancellationTokenSource?.Dispose();
        _sendCancellationTokenSource = new();

        ReceiveSetupActivity.TryGetBtAddress(this, out var btAddress);

        _cdp = new(new()
        {
            Type = DeviceType.Android,
            Name = SettingsFragment.GetDeviceName(this, btAdapter),
            OemModelName = Build.Model ?? string.Empty,
            OemManufacturerName = Build.Manufacturer ?? string.Empty,
            DeviceCertificate = ConnectedDevicesPlatform.CreateDeviceCertificate(CdpEncryptionParams.Default),
            LoggerFactory = loggerFactory
        });

        AndroidBluetoothHandler bluetoothHandler = new(btAdapter, btAddress ?? PhysicalAddress.None);
        _cdp.AddTransport<BluetoothTransport>(new(bluetoothHandler));

        AndroidNetworkHandler networkHandler = new(this);
        _cdp.AddTransport<NetworkTransport>(new(networkHandler));

        if (btAddress != null)
        {
            _receiveCancellationTokenSource?.Dispose();
            _receiveCancellationTokenSource = new();

            _cdp.Listen(_receiveCancellationTokenSource.Token);
            _cdp.Advertise(_receiveCancellationTokenSource.Token);
            NearShareReceiver.Start(_cdp, this);

            _logger.LogInformation("Started receiving using address {btAddress}", btAddress);
        }
        else
        {
            _logger.LogInformation("Not advertising because btAddress is empty");
        }

        _cdp.Discover(_sendCancellationTokenSource.Token);
        _logger.LogInformation("Start discovery", btAddress);

        _instance = this;
        _promise?.TrySetResult();
    }

    public ConnectedDevicesPlatform Platform
        => _cdp ?? throw new InvalidOperationException("Service not initialized");

    public override void OnDestroy()
    {
        _instance = null;
        _promise?.TrySetCanceled();
        _promise = null;

        if (_sendCancellationTokenSource != null)
        {
            _sendCancellationTokenSource.Cancel();
            _sendCancellationTokenSource.Dispose();
            _sendCancellationTokenSource = null;

            _logger?.LogInformation("Stopped discovery");
        }

        if (_receiveCancellationTokenSource != null)
        {
            _receiveCancellationTokenSource.Cancel();
            _receiveCancellationTokenSource.Dispose();
            _receiveCancellationTokenSource = null;

            NearShareReceiver.Stop();
            _logger?.LogInformation("Stopped receiving");
        }

        if (_cdp != null)
        {
            _cdp.Dispose();
            _cdp = null;
        }
    }

    #region Receive
    const string TransferChannelId = "de.shortdev.nearshare.transfer";
    void SendNotification(int id, NotificationCompat.Builder notification)
    {
        NotificationManager manager = (NotificationManager?)GetSystemService(NotificationService) ?? throw new NullReferenceException("Could not get notifcation service");

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            manager.CreateNotificationChannel(new(TransferChannelId, "Data Transfer Receipt", NotificationImportance.High));
            notification.SetChannelId(TransferChannelId);
        }

        manager.Notify(id, notification.Build());
    }

    void INearSharePlatformHandler.OnReceivedUri(UriTransferToken transfer)
    {
        Intent intent = new(Intent.ActionView);
        intent.SetData(AndroidUri.Parse(transfer.Uri));
        var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Mutable | PendingIntentFlags.UpdateCurrent);

        var style = new NotificationCompat.BigTextStyle()
            .SetBigContentTitle($"Receive from {transfer.DeviceName}")
            .SetSummaryText($"{transfer.DeviceName} wants to share a website with you.")
            .BigText(transfer.Uri);

        var notification = new NotificationCompat.Builder(this, TransferChannelId)
            .SetContentTitle($"Receive from {transfer.DeviceName}")
            .SetContentText($"{transfer.DeviceName} wants to share a website with you.")
            .SetStyle(style)
            .SetSmallIcon(Resource.Mipmap.ic_launcher)
            .SetAutoCancel(true)
            .SetContentIntent(pendingIntent);

        SendNotification(0, notification);
    }

    void INearSharePlatformHandler.OnFileTransfer(FileTransferToken transfer)
    {
        if (_handler != null)
        {
            _handler.OnFileTransfer(transfer);
            return;
        }
        transfer.Cancel();
    }
    #endregion

    INearSharePlatformHandler? _handler;
    public void SetReceiveListener(INearSharePlatformHandler? handler)
    {
        _handler = handler;
    }
}
