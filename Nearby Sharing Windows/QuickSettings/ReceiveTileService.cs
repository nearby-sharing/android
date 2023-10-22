using Android.Content;
using Android.Service.QuickSettings;

namespace Nearby_Sharing_Windows.QuickSettings;

[IntentFilter(new[] { ActionQsTile })]
[Service(Label = "Receive", Exported = true, Icon = "@drawable/quick_settings_tile_icon", Permission = "android.permission.BIND_QUICK_SETTINGS_TILE")]
// [MetaData(MetaDataActiveTile, Value = "true")]
public sealed class ReceiveTileService : TileService
{
    public override void OnClick()
    {
        Intent intent = new(this, typeof(ReceiveActivity));
        intent.AddFlags(ActivityFlags.NewTask);

        if (OperatingSystem.IsAndroidVersionAtLeast(24))
            StartActivityAndCollapse(intent);
        else
            StartActivity(intent);
    }
}
