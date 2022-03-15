using Android.App;
using Android.Views;
using Google.Android.Material.BottomSheet;

namespace Nearby_Sharing_Windows.Layout
{
    public class BottomSheetBehaviorCallback : BottomSheetBehavior.BottomSheetCallback
    {
        public Activity Activity { get; private set; }
        public BottomSheetBehaviorCallback(Activity activity)
        {
            this.Activity = activity;
        }

        public override void OnSlide(View bottomSheet, float newState) { }

        public override void OnStateChanged(View p0, int p1)
        {
            if (p1 == BottomSheetBehavior.StateHidden)
                Activity.Finish();
        }
    }
}