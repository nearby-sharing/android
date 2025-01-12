using Android.Content;
using Java.Lang;
using System.Collections;

namespace NearShare.Utils;

internal static class IntentExtensions
{
    public static IList<T>? GetParcelableArrayListExtra<T>(this Intent @this, string? name) where T : Java.Lang.Object
    {
        IList? result;
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            result = @this.GetParcelableArrayListExtra(name, Class.FromType(typeof(T)));
        else
            result = @this.GetParcelableArrayListExtra(name);
        return result?.Cast<T>().ToList();
    }

    public static T? GetParcelableExtra<T>(this Intent @this, string? name) where T : Java.Lang.Object
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return (T?)@this.GetParcelableExtra(name, Class.FromType(typeof(T)));
        else
            return (T?)@this.GetParcelableExtra(name);
    }
}
