using Android.Content;
using AndroidX.Preference;
using NearShare.Receive;
using NearShare.Utils;

namespace NearShare.Settings;

internal sealed class CdpFragment : SettingsFragment
{
    public override void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
    {
        SetPreferencesFromResource(Resource.Xml.preferences_cdp, rootKey);

        PreferenceScreen!.FindPreference("request_permissions_send")!.PreferenceClick +=
            (s, e) => UIHelper.RequestSendPermissions(Activity!);
        PreferenceScreen!.FindPreference("request_permissions_receive")!.PreferenceClick +=
            (s, e) => UIHelper.RequestReceivePermissions(Activity!);

        ((EditTextPreference)PreferenceScreen!.FindPreference("device_name")!).DialogLayoutResource = Resource.Layout.settingslib_preference_dialog_edittext;

        PreferenceScreen!.FindPreference("goto_mac_address")!.PreferenceClick +=
            (s, e) => StartActivity(new Intent(Activity!, typeof(ReceiveSetupFragment)));
        PreferenceScreen!.FindPreference("open_setup")!.PreferenceClick +=
            (s, e) => UIHelper.OpenSetup(Activity!);
    }
}
