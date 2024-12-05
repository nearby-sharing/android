using Android.Animation;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Text;
using Android.Util;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Browser.CustomTabs;
using AndroidX.Core.App;
using Google.Android.Material.Dialog;
using NearShare.Droid.Settings;
using System.Runtime.Versioning;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using CompatToolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace NearShare.Droid;

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
        => DisplayWebSite(activity, "https://github.com/nearby-sharing/android/");

    public static void DisplayWebSite(Activity activity, string url)
    {
        CustomTabsIntent intent = new CustomTabsIntent.Builder()
            .Build();
        intent.LaunchUrl(activity, AndroidUri.Parse(url) ?? throw new ArgumentException("Invalid url", nameof(url)));
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
            Intent intent = new(global::Android.Provider.Settings.ActionAppLocaleSettings);
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

    public static void SetupToolBar(AppCompatActivity activity, string? title = null)
    {
        var toolbar = activity.FindViewById<CompatToolbar>(Resource.Id.toolbar)!;
        activity.SetSupportActionBar(toolbar);
        if (title is not null)
            activity.SupportActionBar!.Title = title;
    }

    #region Permissions
    private static readonly string[] _sendPermissions = [
        ManifestPermission.AccessFineLocation,
        ManifestPermission.AccessCoarseLocation,
        // WiFiDirect
        ManifestPermission.AccessWifiState,
        ManifestPermission.ChangeWifiState
    ];
    [SupportedOSPlatform("android31.0")]
    private static readonly string[] _sendPermissionsApi31 = [
        .. _sendPermissions,
        ManifestPermission.BluetoothScan,
        ManifestPermission.BluetoothConnect
    ];
    [SupportedOSPlatform("android33.0")]
    private static readonly string[] _sendPermissionsApi33 = [
        .. _sendPermissionsApi31,
        ManifestPermission.NearbyWifiDevices
    ];
    public static void RequestSendPermissions(Activity activity)
        => ActivityCompat.RequestPermissions(
                activity,
                OperatingSystem.IsAndroidVersionAtLeast(33) ? _sendPermissionsApi33 :
                (OperatingSystem.IsAndroidVersionAtLeast(31) ? _sendPermissionsApi31 : _sendPermissions),
                0
            );

    private static readonly string[] _receivePermissions = [
        ManifestPermission.AccessFineLocation,
        ManifestPermission.AccessCoarseLocation,
        ManifestPermission.AccessWifiState,
        ManifestPermission.Bluetooth,
        // ManifestPermission.AccessBackgroundLocation, See #109 and #41 // Api 29
        ManifestPermission.ReadExternalStorage,
        ManifestPermission.WriteExternalStorage
    ];
    [SupportedOSPlatform("android31.0")]
    private static readonly string[] _receivePermissionsApi31 = [
        .. _receivePermissions,
        ManifestPermission.BluetoothScan,
        ManifestPermission.BluetoothConnect,
        ManifestPermission.BluetoothAdvertise,
    ];
    public static void RequestReceivePermissions(Activity activity)
        => ActivityCompat.RequestPermissions(
                activity,
                OperatingSystem.IsAndroidVersionAtLeast(31) ? _receivePermissionsApi31 : _receivePermissions,
                0
            );
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

    public static void EnableLayoutTransition(this ViewGroup view, bool animateParentHierarchy = false)
    {
        LayoutTransition transition = new();
        transition.SetAnimateParentHierarchy(animateParentHierarchy);
        view.LayoutTransition = transition;
    }

    public static int GetColorAttr(this Context context, int attr)
    {
        var theme = context.Theme ?? throw new NullReferenceException("Empty theme");

        TypedValue result = new();
        if (!theme.ResolveAttribute(attr, result, resolveRefs: true))
            throw new InvalidOperationException($"Could not resolve attribute {attr}");

        return result.Data;
    }

    public static AlertDialog? ShowErrorDialog(this Context context, Exception ex)
    {
        MaterialAlertDialogBuilder errorDialogBuilder = new(context);
        errorDialogBuilder.SetTitle(ex.GetType().Name);
        errorDialogBuilder.SetMessage(ex.Message);
        errorDialogBuilder.SetNeutralButton("Ok", (s, e) => { });
        return errorDialogBuilder.Show();
    }

    public static void PlaySound(this Context context, int soundId)
    {
        MediaPlayer player = MediaPlayer.Create(context, soundId) ?? throw new NullReferenceException("Could not create MediaPlayer");
        player.Start();
    }
}