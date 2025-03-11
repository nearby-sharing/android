using Android.Content;
using Android.Graphics;
using Android.Service.QuickSettings;
using Android.Views;
using AndroidX.Activity;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using AndroidX.Preference;
using NearShare.Utils;

namespace NearShare.Settings;

[IntentFilter([TileService.ActionQsTilePreferences])]
[Activity(Label = "@string/app_name", Exported = true, Theme = "@style/AppTheme", ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class SettingsActivity : AppCompatActivity, ISettingsNavigation
{
    Stack<SettingsFragment> ISettingsNavigation.NavigationStack { get; } = new();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_settings);
        UIHelper.SetupToolBar(this, GetString(Resource.String.generic_settings));

        SupportActionBar!.SetDisplayShowHomeEnabled(true);
        SupportActionBar!.SetDisplayHomeAsUpEnabled(true);

        var backDrawable = GetDrawable(Resource.Drawable.ic_fluent_arrow_left_24_selector)!;
        backDrawable.SetTint(Color.White.ToArgb());
        SupportActionBar!.SetHomeAsUpIndicator(backDrawable);

        SettingsFragment.NavigateFragment<SettingsHomepageFragment>(SupportFragmentManager, this);

        OnBackPressedDispatcher.AddCallback(this, new BackPressedListener(this, SupportFragmentManager, OnBackPressedDispatcher, true));
    }

    sealed class BackPressedListener(ISettingsNavigation navigation, AndroidX.Fragment.App.FragmentManager fragmentManager, OnBackPressedDispatcher dispatcher, bool enabled) : OnBackPressedCallback(enabled)
    {
        readonly ISettingsNavigation _navigation = navigation;
        readonly AndroidX.Fragment.App.FragmentManager _fragmentManager = fragmentManager;
        readonly OnBackPressedDispatcher _dispatcher = dispatcher;

        public override void HandleOnBackPressed()
        {
            if (_navigation.NavigationStack.Count <= 1)
            {
                Enabled = false;
                _dispatcher.OnBackPressed();
                return;
            }

            _navigation.NavigationStack.Pop();

            var newFragment = _navigation.NavigationStack.Peek();
            SettingsFragment.NavigateFragment(_fragmentManager, newFragment);
        }
    }

    public override bool OnSupportNavigateUp()
    {
        OnBackPressedDispatcher.OnBackPressed();
        return true;
    }

    public override bool OnCreateOptionsMenu(IMenu? menu)
        => UIHelper.OnCreateOptionsMenu(this, menu);

    public override bool OnOptionsItemSelected(IMenuItem item)
        => UIHelper.OnOptionsItemSelected(this, item);
}

sealed class SettingsHomepageFragment : SettingsFragment
{
    public override void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
    {
        SetPreferencesFromResource(Resource.Xml.preferences, rootKey);

        PreferenceScreen!.FindPreference("design_screen")!.PreferenceClick +=
            (s, e) => NavigateFragment<DesignScreenFragment>();
        PreferenceScreen!.FindPreference("cdp_screen")!.PreferenceClick +=
            (s, e) => NavigateFragment<CdpScreenFragment>();

        PreferenceScreen!.FindPreference("open_sponsor")!.PreferenceClick +=
            (s, e) => UIHelper.OpenSponsor(Activity!);
        PreferenceScreen!.FindPreference("open_discord")!.PreferenceClick +=
            (s, e) => UIHelper.OpenDiscord(Activity!);
        PreferenceScreen!.FindPreference("open_faq")!.PreferenceClick +=
            (s, e) => UIHelper.OpenFAQ(Activity!);

        PreferenceScreen!.FindPreference("show_credits")!.PreferenceClick +=
            (s, e) => UIHelper.OpenCredits(Activity!);
        PreferenceScreen!.FindPreference("open_github")!.PreferenceClick +=
            (s, e) => UIHelper.OpenGitHub(Activity!);
    }
}

sealed class DesignScreenFragment : SettingsFragment
{
    public override void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
    {
        SetPreferencesFromResource(Resource.Xml.preferences_design, rootKey);

        PreferenceScreen!.FindPreference("force_dark_mode")!.PreferenceChange += (s, e) =>
        {
            var value = ((Java.Lang.Boolean)e.NewValue!).BooleanValue();
            AppCompatDelegate.DefaultNightMode = value ? AppCompatDelegate.ModeNightYes : AppCompatDelegate.ModeNightFollowSystem;
        };

        PreferenceScreen!.FindPreference("switch_language")!.PreferenceClick +=
            (s, e) => UIHelper.OpenLocaleSettings(Activity!);
    }
}

sealed class CdpScreenFragment : SettingsFragment
{
    public override void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
    {
        SetPreferencesFromResource(Resource.Xml.preferences_cdp, rootKey);

        PreferenceScreen!.FindPreference("request_permissions_send")!.PreferenceClick +=
            (s, e) => ActivityCompat.RequestPermissions(Activity!, UIHelper.GetSendPermissions(), requestCode: 0);
        PreferenceScreen!.FindPreference("request_permissions_receive")!.PreferenceClick +=
            (s, e) => UIHelper.RequestReceivePermissions(Activity!);

        ((EditTextPreference)PreferenceScreen!.FindPreference("device_name")!).DialogLayoutResource = Resource.Layout.settingslib_preference_dialog_edittext;

        PreferenceScreen!.FindPreference("goto_mac_address")!.PreferenceClick +=
            (s, e) => StartActivity(new Intent(Activity!, typeof(ReceiveSetupActivity)));
        PreferenceScreen!.FindPreference("open_setup")!.PreferenceClick +=
            (s, e) => UIHelper.OpenSetup(Activity!);
    }
}
