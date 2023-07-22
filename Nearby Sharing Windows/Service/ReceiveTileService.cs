using Android.Content;
using Android.Service.QuickSettings;

namespace Nearby_Sharing_Windows.Service;

[IntentFilter(new[] { ActionQsTile })]
[Service(Label = "Receive", Exported = true, Icon = "@drawable/quick_settings_tile_icon", Permission = "android.permission.BIND_QUICK_SETTINGS_TILE")]
[MetaData(MetaDataActiveTile, Value = "true")]
public sealed class ReceiveTileService : TileService
{
    public override void OnStartListening()
    {
        IServiceSingleton<CdpService>.StateChanged += OnCdpServiceStateChanged;
        IServiceSingleton<CdpService>.CallEvent(OnCdpServiceStateChanged);
    }

    void OnCdpServiceStateChanged(CdpService? instance, bool running)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(24) || QsTile == null)
            return;

        if (!running)
        {
            QsTile.State = TileState.Unavailable;
        }
        else
        {
            QsTile.State = TileState.Active;
        }
        QsTile.UpdateTile();
    }

    public override void OnStopListening()
    {
        IServiceSingleton<CdpService>.StateChanged -= OnCdpServiceStateChanged;
    }

    public override void OnClick()
    {
        Intent intent = new(this, typeof(ReceiveActivity));
        intent.AddFlags(ActivityFlags.NewTask);

        if (OperatingSystem.IsAndroidVersionAtLeast(24))
            StartActivityAndCollapse(intent);
        else
            StartActivity(intent);

        if (!OperatingSystem.IsAndroidVersionAtLeast(24) || QsTile == null)
            return;

        QsTile.UpdateTile();
    }
}
