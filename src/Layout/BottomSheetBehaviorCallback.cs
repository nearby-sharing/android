using Android.Views;
using Google.Android.Material.BottomSheet;

namespace NearShare.Droid.Layout;

public class BottomSheetBehaviorCallback(Activity activity) : BottomSheetBehavior.BottomSheetCallback
{
    public Activity Activity { get; private set; } = activity;

    public override void OnSlide(View bottomSheet, float newState) { }

    public override void OnStateChanged(View p0, int p1)
    {
        if (p1 == BottomSheetBehavior.StateHidden)
            Activity.Finish();
    }
}