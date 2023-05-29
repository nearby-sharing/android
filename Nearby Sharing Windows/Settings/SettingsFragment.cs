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
        => NavigateFragment<TFragment>(ParentFragmentManager);

    public static void NavigateFragment<TFragment>(AndroidX.Fragment.App.FragmentManager manager) where TFragment : SettingsFragment, new()
    {
        manager.BeginTransaction()
            .Replace(Resource.Id.settings_container, new TFragment())
            .Commit();
    }

    public static ISharedPreferences GetSettings()
        => Application.Context.GetSharedPreferences("settings", Android.Content.FileCreationMode.Private)!;
}
