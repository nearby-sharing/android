using Android.Content;
using Android.Service.QuickSettings;

namespace NearShare.Receive;

[IntentFilter([ActionQsTile])]
[Service(Label = "Receive", Exported = true, Icon = "@drawable/quick_settings_tile_icon", Permission = "android.permission.BIND_QUICK_SETTINGS_TILE")]
// [MetaData(MetaDataActiveTile, Value = "true")]
public sealed class ReceiveTileService : TileService
{
    public override void OnClick()
    {
        Intent intent = new(this, typeof(ReceiveFragment));
        intent.AddFlags(ActivityFlags.NewTask);

        if (OperatingSystem.IsAndroidVersionAtLeast(34))
            StartActivityAndCollapse(
                PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable) ?? throw new InvalidOperationException("Could not create PendingIntent")
            );
        else if (OperatingSystem.IsAndroidVersionAtLeast(24))
            StartActivityAndCollapse(intent);
        else
            StartActivity(intent);
    }
}
