using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;
using NearShare.Droid.Settings;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;
using ShortDev.Microsoft.ConnectedDevices.Transports.Network;
using System.Net.NetworkInformation;

namespace NearShare.Droid.Service;

[Service(Exported = true)]
public sealed class CdpService : Android.App.Service
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

    CancellationTokenSource? _discoveryCancellation;
    ConnectedDevicesPlatform? _cdp;
    ILogger? _logger;
    public override void OnCreate()
    {
        var loggerFactory = ConnectedDevicesPlatform.CreateLoggerFactory(this.GetLogFilePattern());
        _logger = loggerFactory.CreateLogger<CdpService>();

        var service = (BluetoothManager)GetSystemService(BluetoothService)!;
        var btAdapter = service.Adapter ?? throw new NullReferenceException("Could not get bt adapter");

        _discoveryCancellation?.Dispose();
        _discoveryCancellation = new();

        ReceiveSetupActivity.TryGetBtAddress(this, out var btAddress);

        _cdp = new(new()
        {
            Type = DeviceType.Android,
            Name = SettingsFragment.GetDeviceName(this, btAdapter),
            OemModelName = Build.Model ?? string.Empty,
            OemManufacturerName = Build.Manufacturer ?? string.Empty,
            DeviceCertificate = ConnectedDevicesPlatform.CreateDeviceCertificate(CdpEncryptionParams.Default)
        }, loggerFactory);

        AndroidBluetoothHandler bluetoothHandler = new(btAdapter, btAddress ?? PhysicalAddress.None);
        _cdp.AddTransport<BluetoothTransport>(new(bluetoothHandler));

        AndroidNetworkHandler networkHandler = new(this);
        _cdp.AddTransport<NetworkTransport>(new(networkHandler));

        _cdp.Discover(_discoveryCancellation.Token);
        _logger.LogInformation("Start discovery");

        _instance = this;
    }

    public ConnectedDevicesPlatform Platform
        => _cdp ?? throw new InvalidOperationException("Service not initialized");

    public override void OnDestroy()
    {
        _instance = null;

        if (_discoveryCancellation != null)
        {
            _discoveryCancellation.Cancel();
            _discoveryCancellation.Dispose();
            _discoveryCancellation = null;

            _logger?.LogInformation("Stopped discovery");
        }

        if (_cdp != null)
        {
            _cdp.Dispose();
            _cdp = null;
        }
    }

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
}
