namespace NearShare.Droid.Settings;

internal interface ISettingsNavigation
{
    Stack<SettingsFragment> NavigationStack { get; }
}
