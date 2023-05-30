using Android.Runtime;
using AndroidX.AppCompat.App;
using Nearby_Sharing_Windows.Settings;

namespace Nearby_Sharing_Windows;

[Application]
public sealed class App : Application
{
    public App(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();

        AppCompatDelegate.DefaultNightMode = SettingsFragment.ShouldForceDarkMode(this) ? AppCompatDelegate.ModeNightYes : AppCompatDelegate.ModeNightFollowSystem;
    }
}
