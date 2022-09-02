using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.AppBar;
using Google.Android.Material.FloatingActionButton;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;

namespace Nearby_Sharing_Windows
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            MaterialToolbar toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Nearby Sharing";
            SupportActionBar.Subtitle = "Send data to Windows 10 / 11";

            //FloatingActionButton openNearbySharingFab = FindViewById<FloatingActionButton>(Resource.Id.open_nearby_share_android_fab);
            //openNearbySharingFab.Click += delegate
            //{
            //    Intent intent = new Intent("com.google.android.gms.SHARE_NEARBY");
            //    intent.SetPackage("com.google.android.gms");
            //    StartActivity(intent);
            //};

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.floating_action_button);
            fab.Click += SendButton_Click;

            Button sendButton = FindViewById<Button>(Resource.Id.sendButton);
            sendButton.Click += SendButton_Click;

            StartActivity(new Intent(this, typeof(ReceiveActivity)));
        }

        const int FilePickCode = 0x1;
        private void SendButton_Click(object sender, System.EventArgs e)
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
