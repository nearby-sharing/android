using NearShare.Utils;

namespace NearShare.Settings;

internal sealed class SettingsHomepageFragment : SettingsFragment
{
    public override void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
    {
        SetPreferencesFromResource(Resource.Xml.preferences, rootKey);

        PreferenceScreen!.FindPreference("design_screen")!.PreferenceClick +=
            (s, e) => this.NavController.Navigate(Routes.SettingsAppearance);
        PreferenceScreen!.FindPreference("cdp_screen")!.PreferenceClick +=
            (s, e) => this.NavController.Navigate(Routes.SettingsCdp);

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
