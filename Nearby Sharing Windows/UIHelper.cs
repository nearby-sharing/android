using Android.Content;
using Android.Content.PM;
using Android.Text;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Browser.CustomTabs;
using AndroidX.Core.App;
using Google.Android.Material.Dialog;
using Nearby_Sharing_Windows.Settings;
using CompatToolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace Nearby_Sharing_Windows;

internal static class UIHelper
{
    public const ConfigChanges ConfigChangesFlags = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density;

    public static bool OnCreateOptionsMenu(Activity activity, IMenu? menu)
    {
        activity.MenuInflater.Inflate(Resource.Menu.menu_main, menu);
        return true;
    }

    public static bool OnOptionsItemSelected(Activity activity, IMenuItem item)
    {
        switch (item.ItemId)
        {
            case Resource.Id.action_help:
                OpenFAQ(activity);
                return true;
            case Resource.Id.action_sponsor:
                OpenSponsor(activity);
                return true;
            case Resource.Id.action_settings:
                activity.StartActivity(new Intent(activity, typeof(SettingsActivity)));
                return true;
        }
        return false;
    }

    public static void OpenFAQ(Activity activity)
        => DisplayWebSite(activity, "https://nearshare.shortdev.de/docs/FAQ");

    public static void OpenSponsor(Activity activity)
        => DisplayWebSite(activity, "https://nearshare.shortdev.de/docs/sponsor");

    public static void OpenDiscord(Activity activity)
        => DisplayWebSite(activity, "https://nearshare.shortdev.de/docs/discord");

    public static void OpenSetup(Activity activity)
        => DisplayWebSite(activity, "https://nearshare.shortdev.de/docs/setup");

    public static void OpenCredits(Activity activity)
        => DisplayWebSite(activity, "https://nearshare.shortdev.de/CREDITS");

    public static void OpenGitHub(Activity activity)
        => DisplayWebSite(activity, "https://github.com/ShortDevelopment/Nearby-Sharing-Windows/");

    public static void DisplayWebSite(Activity activity, string url)
    {
        CustomTabsIntent intent = new CustomTabsIntent.Builder()
            .Build();
        intent.LaunchUrl(activity, AndroidUri.Parse(url));
    }

    public static void OpenLocaleSettings(Activity activity)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            new MaterialAlertDialogBuilder(activity)
                .SetMessage("Only supported on Android 13+")!
                .SetNeutralButton("Ok", (s, e) => { })!
                .Show();
            return;
        }

        try
        {
            Intent intent = new(Android.Provider.Settings.ActionAppLocaleSettings);
            intent.SetData(AndroidUri.FromParts("package", activity.PackageName, null));
            activity.StartActivity(intent);
        }
        catch (Exception ex)
        {
            new MaterialAlertDialogBuilder(activity)
                .SetMessage("Your phone does not support language settings!\n" + ex.Message)!
                .SetNeutralButton("Ok", (s, e) => { })!
                .Show();
        }
    }

    public static void ViewDownloads(this Activity activity)
    {
        Intent intent = new(DownloadManager.ActionViewDownloads);
        activity.StartActivity(intent);
    }

    public static void SetupToolBar(AppCompatActivity activity, string? subtitle = null)
    {
        var toolbar = activity.FindViewById<CompatToolbar>(Resource.Id.toolbar)!;
        activity.SetSupportActionBar(toolbar);
        activity.SupportActionBar!.Subtitle = subtitle;
    }

    #region Permissions
    private static readonly string[] _sendPermissions = new[]
    {
        ManifestPermission.AccessFineLocation,
        ManifestPermission.AccessCoarseLocation,
        // Api level 31
        ManifestPermission.BluetoothScan,
        ManifestPermission.BluetoothConnect
    };
    public static void RequestSendPermissions(Activity activity)
        => ActivityCompat.RequestPermissions(activity, _sendPermissions, 0);

    private static readonly string[] _receivePermissions = new[]
    {
        ManifestPermission.AccessFineLocation,
        ManifestPermission.AccessCoarseLocation,
        ManifestPermission.AccessWifiState,
        ManifestPermission.Bluetooth,
        ManifestPermission.BluetoothScan,
        ManifestPermission.BluetoothConnect,
        ManifestPermission.BluetoothAdvertise,
        // ManifestPermission.AccessBackgroundLocation, See #109 and #41
        ManifestPermission.ReadExternalStorage,
        ManifestPermission.WriteExternalStorage
    };
    public static void RequestReceivePermissions(Activity activity)
        => ActivityCompat.RequestPermissions(activity, _receivePermissions, 0);
    #endregion

    public static ISpanned LoadHtmlAsset(Activity activity, string assetPath)
    {
        string langCode = activity.GetString(Resource.String.assets_prefix);
        string fileName = $"{assetPath}.html";
        if (!activity.Assets!.List($"{langCode}/")!.Contains(fileName))
            langCode = "en";

        using var stream = activity.Assets!.Open($"{langCode}/{fileName}");
        using StreamReader reader = new(stream);

        ISpanned? result;
        if (OperatingSystem.IsAndroidVersionAtLeast(24))
            result = Html.FromHtml(reader.ReadToEnd(), FromHtmlOptions.ModeLegacy);
        else
            result = Html.FromHtml(reader.ReadToEnd());
        return result ?? throw new NullReferenceException("\"Html.FromHtml\" returned \"null\"");
    }

    public static string Localize(this Activity activity, int resId, params object[] args)
        => string.Format(activity.GetString(resId), args);
}