namespace NearShare.Settings;

internal interface ISettingsNavigation
{
    Stack<SettingsFragment> NavigationStack { get; }
}
