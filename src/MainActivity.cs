using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Service.QuickSettings;
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using AndroidX.Navigation;
using AndroidX.Navigation.UI;
using Google.Android.Material.BottomNavigation;
using NearShare.Utils;

namespace NearShare;

[IntentFilter([TileService.ActionQsTilePreferences])]
[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class MainActivity : AppCompatActivity
{
    NavController NavController => field ??= SupportFragmentManager.FindFragmentById(Resource.Id.nav_host_fragment)!.NavController;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        WindowCompat.EnableEdgeToEdge(Window);

        SetContentView(Resource.Layout.activity_main);
        SetSupportActionBar(FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar));

        if (savedInstanceState is not null)
            return;

        NavController.Graph = NavController.CreateGraph(id: 42, startDestination: Routes.Home, graph =>
        {
            graph.Fragment<HomeFragment>(Routes.Home, builder =>
            {
                builder.Label = GetString(Resource.String.app_name);
            });

            graph.Fragment<Receive.ReceiveFragment>(Routes.Receive, builder =>
            {
                builder.Label = GetString(Resource.String.generic_receive);
            });
            graph.Fragment<Receive.ReceiveSetupFragment>(Routes.ReceiveSetup, builder =>
            {
                builder.Label = GetString(Resource.String.app_titlebar_title_receive_setup);
            });

            graph.Fragment<Settings.SettingsHomepageFragment>(Routes.Settings, builder =>
            {
                builder.Label = GetString(Resource.String.generic_settings);
            });
            graph.Fragment<Settings.AppearanceFragment>(Routes.SettingsAppearance, builder =>
            {
                builder.Label = GetString(Resource.String.preference_appearance_title);
            });
            graph.Fragment<Settings.CdpFragment>(Routes.SettingsCdp, builder =>
            {
                builder.Label = GetString(Resource.String.preference_cdp_title);
            });
        });
        NavigationUI.SetupActionBarWithNavController(this, NavController);
        NavigationUI.SetupWithNavController(FindViewById<BottomNavigationView>(Resource.Id.bottom_navigation)!, NavController);
    }

    public override bool OnSupportNavigateUp() => NavController.NavigateUp() || base.OnSupportNavigateUp();

    public const int FilePickCode = 0x1;
    protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent? data)
    {
        if (resultCode != Result.Ok || data == null)
            return;

        if (requestCode == FilePickCode)
        {
            Intent intent = new(this, typeof(SendActivity));

            var clipData = data.ClipData;
            if (data.Data != null)
            {
                intent.SetAction(Intent.ActionSend);
                intent.PutExtra(Intent.ExtraStream, data.Data);
            }
            else if (clipData != null)
            {
                intent.SetAction(Intent.ActionSendMultiple);

                List<IParcelable> uriList = [];
                for (int i = 0; i < clipData.ItemCount; i++)
                {
                    var item = clipData.GetItemAt(i);
                    if (item?.Uri == null)
                        continue;

                    uriList.Add(item.Uri);
                }

                intent.PutParcelableArrayListExtra(Intent.ExtraStream, uriList);
            }
            else
                return;

            StartActivity(intent);
        }
    }
}
