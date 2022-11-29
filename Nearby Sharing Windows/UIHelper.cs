using Android.Content;
using Android.Content.PM;
using Android.Text;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Browser.CustomTabs;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using CompatToolbar = AndroidX.AppCompat.Widget.Toolbar;
using ManifestPermission = Android.Manifest.Permission;

namespace Nearby_Sharing_Windows;

internal static class UIHelper
{
    public const ConfigChanges ConfigChangesFlags = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density;

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

    public static void DisplayWebSite(Activity activity, string url)
    {
        CustomTabsIntent intent = new CustomTabsIntent.Builder()
            .Build();
        intent.LaunchUrl(activity, Android.Net.Uri.Parse(url));
    }

    public static void OpenFile(Activity activity, string path)
    {
        Intent intent = new(Intent.ActionView);
        var contentUri = FileProvider.GetUriForFile(activity, "de.shortdev.nearshare.FileProvider", new Java.IO.File(path))!;

        var mimeType = activity.ContentResolver?.GetType(contentUri);
        if (string.IsNullOrEmpty(mimeType))
            intent.SetData(contentUri);
        else
            intent.SetDataAndType(contentUri, mimeType);

        intent.SetFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
        var chooserIntent = Intent.CreateChooser(intent, $"Open {Path.GetFileName(path)}");
        activity.StartActivity(chooserIntent);
    }

    public static void SetupToolBar(AppCompatActivity activity, string? subtitle = null)
    {
        var toolbar = activity.FindViewById<CompatToolbar>(Resource.Id.toolbar)!;
        activity.SetSupportActionBar(toolbar);
        activity.SupportActionBar!.Subtitle = subtitle;
    }

    public static void RequestReceivePermissions(Activity activity)
    {
        ActivityCompat.RequestPermissions(activity, new[] {
            ManifestPermission.AccessFineLocation,
            ManifestPermission.AccessCoarseLocation,
            ManifestPermission.Bluetooth,
            ManifestPermission.BluetoothScan,
            ManifestPermission.BluetoothConnect,
            ManifestPermission.BluetoothAdvertise,
            ManifestPermission.AccessBackgroundLocation,
            ManifestPermission.ReadExternalStorage,
            ManifestPermission.WriteExternalStorage
        }, 0);
    }

    public static ISpanned LoadHtmlAsset(Activity activity, string assetPath)
    {
        using (var stream = activity.Assets!.Open(assetPath))
        using (StreamReader reader = new(stream))
        {
            return Html.FromHtml(reader.ReadToEnd())!;
        }
    }
}