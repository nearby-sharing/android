using Android.Bluetooth;
using Android.Content;
using AndroidX.Preference;

namespace NearShare.Settings;

internal abstract class SettingsFragment : PreferenceFragmentCompat
{
    public static bool ShouldForceDarkMode(Context context)
        => PreferenceManager.GetDefaultSharedPreferences(context)!.GetBoolean("force_dark_mode", false);

    public static string GetDeviceName(Context context, BluetoothAdapter adapter)
    {
        var deviceName = PreferenceManager.GetDefaultSharedPreferences(context)!.GetString("device_name", null);
        if (string.IsNullOrEmpty(deviceName))
            deviceName = adapter.Name;

        return deviceName ?? Environment.MachineName;
    }
}
