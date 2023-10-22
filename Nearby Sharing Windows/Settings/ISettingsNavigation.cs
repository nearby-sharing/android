namespace Nearby_Sharing_Windows.Settings;

internal interface ISettingsNavigation
{
    Stack<SettingsFragment> NavigationStack { get; }
}
