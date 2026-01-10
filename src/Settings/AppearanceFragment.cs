using AndroidX.AppCompat.App;
using NearShare.Utils;

namespace NearShare.Settings;

internal sealed class AppearanceFragment : SettingsFragment
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
