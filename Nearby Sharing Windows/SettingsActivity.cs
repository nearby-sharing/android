using AndroidX.AppCompat.App;
using AndroidX.Preference;

namespace Nearby_Sharing_Windows
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class SettingsActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_settings);

            SupportFragmentManager
                .BeginTransaction()
                .Replace(Resource.Id.settings_container, new SettingsFragment())
                .Commit();
        }

        class SettingsFragment : PreferenceFragmentCompat
        {
            public override void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
                => SetPreferencesFromResource(Resource.Xml.preferences, rootKey);
        }
    }
}
