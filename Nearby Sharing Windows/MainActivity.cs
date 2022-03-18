using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using Google.Android.Material.FloatingActionButton;

namespace Nearby_Sharing_Windows
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Nearby Sharing";
            SupportActionBar.Subtitle = "Send data to Windows 10 / 11";

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.floating_action_button);
            fab.Click += Fab_Click;
        }

        const int FilePickCode = 0x1;
        private void Fab_Click(object sender, System.EventArgs e)
        {
            StartActivityForResult(
                new Intent(Intent.ActionOpenDocument)
                    .SetType("*/*")
                    .AddCategory(Intent.CategoryOpenable)
                    .PutExtra(Intent.ExtraAllowMultiple, false),
                FilePickCode
            );
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode == FilePickCode && resultCode == Result.Ok && data.Data != null)
            {
                Intent intent = new Intent(this, typeof(ShareTargetSelectActivity));
                intent.SetAction(Intent.ActionSend);
                intent.PutExtra(Intent.ExtraStream, data.Data);
                StartActivity(intent);
            }
        }
    }
}
