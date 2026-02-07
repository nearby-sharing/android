using Android.Content;
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
}
