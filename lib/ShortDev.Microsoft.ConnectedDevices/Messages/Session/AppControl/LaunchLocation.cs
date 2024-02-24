namespace ShortDev.Microsoft.ConnectedDevices.Messages.Session.AppControl;

public enum LaunchLocation : short
{
    /// <summary>
    /// The launched title occupies the full screen.
    /// </summary>
    Full = 0,
    /// <summary>
    /// The launched title occupies most of the screen, sharing it with a snapped-location title.
    /// </summary>
    Fill = 1,
    /// <summary>
    /// The launched title occupies a small column on the left or right of the screen.
    /// </summary>
    Snapped = 2,
    /// <summary>
    /// The launched title is in the start view.
    /// </summary>
    StartView = 3,
    /// <summary>
    /// The launched title is the system UI.
    /// </summary>
    SystemUI = 4,
    /// <summary>
    /// The active title is in its default location
    /// </summary>
    Default = 5
}
