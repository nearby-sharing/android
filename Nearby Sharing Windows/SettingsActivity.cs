using Android.Graphics;
using Android.Service.QuickSettings;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Preference;

namespace Nearby_Sharing_Windows;

[IntentFilter(new[] { TileService.ActionQsTilePreferences })]
[Activity(Label = "@string/app_name", Exported = true, Theme = "@style/AppTheme", ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class SettingsActivity : AppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        SentryHelper.EnsureInitialized();

        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_settings);
        UIHelper.SetupToolBar(this, GetString(Resource.String.generic_settings));

        SupportActionBar!.SetDisplayShowHomeEnabled(true);
        SupportActionBar!.SetDisplayHomeAsUpEnabled(true);

        var backDrawable = GetDrawable(Resource.Drawable.ic_fluent_arrow_left_24_selector)!;
        backDrawable.SetTint(Color.White.ToArgb());
        SupportActionBar!.SetHomeAsUpIndicator(backDrawable);

        SupportFragmentManager
            .BeginTransaction()
            .Replace(Resource.Id.settings_container, new SettingsFragment())
            .Commit();
    }

    public override bool OnSupportNavigateUp()
    {
        OnBackPressedDispatcher.OnBackPressed();
        return true;
    }

    sealed class SettingsFragment : PreferenceFragmentCompat
    {
        public override void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
        {
            SetPreferencesFromResource(Resource.Xml.preferences, rootKey);
            var screen = PreferenceScreen!;
            var activity = Activity!;

            screen.FindPreference("request_permissions")!.PreferenceClick += (s, e) => UIHelper.RequestReceivePermissions(activity);
            screen.FindPreference("open_sponsor")!.PreferenceClick += (s, e) => UIHelper.OpenSponsor(activity);
            screen.FindPreference("open_faq")!.PreferenceClick += (s, e) => UIHelper.OpenFAQ(activity);

            screen.FindPreference("goto_mac_address")!.PreferenceClick += (s, e)
                => StartActivity(new Android.Content.Intent(activity, typeof(ReceiveSetupActivity)));
        }
    }

    public override bool OnCreateOptionsMenu(IMenu? menu)
        => UIHelper.OnCreateOptionsMenu(this, menu);

    public override bool OnOptionsItemSelected(IMenuItem item)
        => UIHelper.OnOptionsItemSelected(this, item);
}
