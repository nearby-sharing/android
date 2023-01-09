using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Preference;

namespace Nearby_Sharing_Windows;

[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class SettingsActivity : AppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_settings);
        UIHelper.SetupToolBar(this, "Settings");

        SupportFragmentManager
            .BeginTransaction()
            .Replace(Resource.Id.settings_container, new SettingsFragment())
            .Commit();
    }

    class SettingsFragment : PreferenceFragmentCompat
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
