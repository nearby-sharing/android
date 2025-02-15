using Android.Runtime;
using AndroidX.AppCompat.App;
using Google.Android.Material.Color;
using Microsoft.Extensions.Logging;
using NearShare.Settings;
using NearShare.Utils;
using ShortDev.Microsoft.ConnectedDevices;
using System.Diagnostics.CodeAnalysis;

[assembly: UsesPermission(ManifestPermission.Bluetooth)]
[assembly: UsesPermission(ManifestPermission.BluetoothAdmin)]
[assembly: UsesPermission(ManifestPermission.BluetoothScan)]
[assembly: UsesPermission(ManifestPermission.BluetoothConnect)]
[assembly: UsesPermission(ManifestPermission.BluetoothAdvertise)]

[assembly: UsesPermission(ManifestPermission.Internet)]
[assembly: UsesPermission(ManifestPermission.AccessNetworkState)]
[assembly: UsesPermission(ManifestPermission.AccessWifiState)]

[assembly: UsesPermission(ManifestPermission.AccessFineLocation)]
[assembly: UsesPermission(ManifestPermission.AccessCoarseLocation)]

[assembly: UsesPermission(ManifestPermission.WriteExternalStorage)]
[assembly: UsesPermission(ManifestPermission.ReadExternalStorage)]

[assembly: UsesFeature("android.hardware.bluetooth", Required = false)]
[assembly: UsesFeature("android.hardware.bluetooth_le", Required = false)]

namespace NearShare;

[Application]
public sealed class App : Application
{
    public App(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
        SentrySdk.Init(options =>
        {
            options.Dsn = "https://47f9f6c3642149a5af942e8484e64fe1@o646413.ingest.sentry.io/6437134";
            options.TracesSampleRate = 0.7;
        });
    }

    public override void OnCreate()
    {
        base.OnCreate();

        AppCompatDelegate.DefaultNightMode = SettingsFragment.ShouldForceDarkMode(this) ? AppCompatDelegate.ModeNightYes : AppCompatDelegate.ModeNightFollowSystem;

        DynamicColors.ApplyToActivitiesIfAvailable(this);
    }

    [field: MaybeNull]
    public static ILoggerFactory LoggerFactory => field ??= CdpUtils.CreateLoggerFactory(Context);

    [field: MaybeNull]
    public static ConnectedDevicesPlatform Platform => field ??= CdpUtils.Create(Context, LoggerFactory);
}
