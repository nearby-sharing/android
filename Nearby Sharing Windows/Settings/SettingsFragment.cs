using Android.Bluetooth;
using Android.Content;
using Android.Views;
using AndroidX.Preference;
using Google.Android.Material.Theme.Overlay;

namespace Nearby_Sharing_Windows.Settings;

internal abstract class SettingsFragment : PreferenceFragmentCompat
{
    public override LayoutInflater OnGetLayoutInflater(Bundle? savedInstanceState)
    {
        var inflater = base.OnGetLayoutInflater(savedInstanceState);
        var wrappedContext = MaterialThemeOverlay.Wrap(RequireContext(), null, 0, Resource.Style.ThemeOverlay_Material3_Light);
        return inflater.CloneInContext(wrappedContext) ?? throw new NullReferenceException("Could not get LayoutInflator");
    }

    protected void NavigateFragment<TFragment>() where TFragment : SettingsFragment, new()
        => NavigateFragment<TFragment>(ParentFragmentManager, Activity as ISettingsNavigation);

    public static void NavigateFragment<TFragment>(AndroidX.Fragment.App.FragmentManager manager, ISettingsNavigation? navigation = null) where TFragment : SettingsFragment, new()
        => NavigateFragment(manager, new TFragment(), navigation);

    public static void NavigateFragment(AndroidX.Fragment.App.FragmentManager manager, SettingsFragment fragment, ISettingsNavigation? navigation = null)
    {
        navigation?.NavigationStack.Push(fragment);

        manager.BeginTransaction()
            .Replace(Resource.Id.settings_container, fragment)
            .Commit();
    }

    public static bool ShouldForceDarkMode(Context context)
        => PreferenceManager.GetDefaultSharedPreferences(context)!.GetBoolean("force_dark_mode", false);

    public static string GetDeviceName(Context context, BluetoothAdapter adapter)
    {
        var deviceName = PreferenceManager.GetDefaultSharedPreferences(context)!.GetString("device_name", null);
        if (string.IsNullOrEmpty(deviceName))
            deviceName = adapter.Name;

        return deviceName ?? throw new NullReferenceException("Could not find device name");
    }
}
