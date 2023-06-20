using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using Nearby_Sharing_Windows.Settings;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Net.NetworkInformation;

namespace Nearby_Sharing_Windows.Service;

[Service]
public sealed class CdpService : Android.App.Service, INearSharePlatformHandler
{
    public override IBinder? OnBind(Intent? intent)
        => new CdpServiceBinder(this);

    [return: GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
    {
        return StartCommandResult.Sticky;
    }

    static bool _isRunning = false;
    public static void EnsureRunning(Context context)
    {
        if (_isRunning)
            return;

        context.StartService(new Intent(context, typeof(CdpService)));
    }

    CancellationTokenSource? _cancellationTokenSource;
    ConnectedDevicesPlatform? _cdp;
    BluetoothAdapter? _adapter;
    public override void OnCreate()
    {
        var service = (BluetoothManager)GetSystemService(BluetoothService)!;
        _adapter = service.Adapter ?? throw new NullReferenceException("Could not get bt adapter");

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new();

        ReceiveSetupActivity.TryGetBtAddress(this, out var btAddress);

        _cdp = new(new()
        {
            Type = DeviceType.Android,
            Name = SettingsFragment.GetDeviceName(this, _adapter),
            OemModelName = Build.Model ?? string.Empty,
            OemManufacturerName = Build.Manufacturer ?? string.Empty,
            DeviceCertificate = ConnectedDevicesPlatform.CreateDeviceCertificate(CdpEncryptionParams.Default),
            LoggerFactory = ConnectedDevicesPlatform.CreateLoggerFactory(msg => System.Diagnostics.Debug.Print(msg))
        });

        AndroidBluetoothHandler bluetoothHandler = new(_adapter, btAddress ?? PhysicalAddress.None);
        _cdp.AddTransport<BluetoothTransport>(new(bluetoothHandler));

        AndroidNetworkHandler networkHandler = new(this);
        _cdp.AddTransport<NetworkTransport>(new(networkHandler));

        if (btAddress != null)
        {
            _cdp.Listen(_cancellationTokenSource.Token);
            _cdp.Advertise(_cancellationTokenSource.Token);
            NearShareReceiver.Start(_cdp, this);
        }
        _cdp.Discover(_cancellationTokenSource.Token);

        _isRunning = true;
    }

    public ConnectedDevicesPlatform Platform
        => _cdp ?? throw new InvalidOperationException("Service not initialized");

    public override void OnDestroy()
    {
        _isRunning = false;

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
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
