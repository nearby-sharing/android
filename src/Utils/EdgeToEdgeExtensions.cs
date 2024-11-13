using Android.Graphics;
using AndroidX.Activity;

namespace NearShare.Droid.Utils;

internal static class EdgeToEdgeExtensions
{
    public static void EnableEdgeToEdge(this ComponentActivity activity)
    {
        EdgeToEdge.Enable(
            activity,
            SystemBarStyle.Dark(Color.Transparent.ToArgb()),
            SystemBarStyle.Auto(Color.Transparent.ToArgb(), Color.Transparent.ToArgb())
        );
    }
}
