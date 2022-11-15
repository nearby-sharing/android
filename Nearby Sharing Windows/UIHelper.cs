using Android.Content;
using Android.Content.PM;
using Android.Views;
using AndroidX.Browser.CustomTabs;

namespace Nearby_Sharing_Windows;

internal static class UIHelper
{
    public const ConfigChanges ConfigChangesFlags = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density;

    public static bool OnOptionsItemSelected(Activity activity, IMenuItem item)
    {
        switch (item.ItemId)
        {
            case Resource.Id.action_help:
                DisplayWebSite(activity, "https://nearshare.shortdev.de/docs/FAQ");
                return true;
            case Resource.Id.action_sponsor:
                DisplayWebSite(activity, "https://nearshare.shortdev.de/docs/sponsor");
                return true;
            case Resource.Id.action_settings:
                activity.StartActivity(new Intent(activity, typeof(SettingsActivity)));
                return true;
        }
        return false;
    }

    public static void DisplayWebSite(Activity activity, string url)
    {
        CustomTabsIntent intent = new CustomTabsIntent.Builder()
            .Build();
        intent.LaunchUrl(activity, Android.Net.Uri.Parse(url));
    }
}