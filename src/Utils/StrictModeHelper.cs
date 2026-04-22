#if DEBUG

using Android.OS;
using static Android.OS.StrictMode;

namespace NearShare.Utils;

internal static class StrictModeHelper
{
    public static void EnableStrictMode()
    {
        SetThreadPolicy(
            new ThreadPolicy.Builder()
                .DetectAll()
                .PenaltyLog()
                .PenaltyFlashScreen()
                .Build()
        );
        SetVmPolicy(
            new VmPolicy.Builder()
                .DetectAll()
                .PenaltyLog()
                .Build()
        );
    }
}

#endif
