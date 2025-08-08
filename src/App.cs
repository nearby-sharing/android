using Android.Runtime;
using AndroidX.AppCompat.App;
using Google.Android.Material.Color;
using NearShare.Settings;
using Rive.Android.Core;
using RiveCore = Rive.Android.Core.Rive;

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
[MetaData(name: "io.sentry.additional-context", Value = "false")]
public sealed class App(nint javaReference, JniHandleOwnership transfer) : Application(javaReference, transfer)
{
    public override void OnCreate()
    {
        base.OnCreate();

        SentrySdk.Init(options =>
        {
            options.Dsn = "https://47f9f6c3642149a5af942e8484e64fe1@o646413.ingest.sentry.io/6437134";
            options.TracesSampleRate = 0.7;
#if DEBUG
            options.Debug = true;
#endif
        });

        AppCompatDelegate.DefaultNightMode = SettingsFragment.ShouldForceDarkMode(this) ? AppCompatDelegate.ModeNightYes : AppCompatDelegate.ModeNightFollowSystem;

        DynamicColors.ApplyToActivitiesIfAvailable(this);

        try
        {
            RiveCore.Instance.Init(ApplicationContext!, defaultRenderer: RendererType.Rive!);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
}
