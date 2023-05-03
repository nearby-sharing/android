using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;

namespace Nearby_Sharing_Windows;

[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class MainActivity : AppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        SentryHelper.EnsureInitialized();

        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);

        //FloatingActionButton openNearbySharingFab = FindViewById<FloatingActionButton>(Resource.Id.open_nearby_share_android_fab);
        //openNearbySharingFab.Click += delegate
        //{
        //    Intent intent = new Intent("com.google.android.gms.SHARE_NEARBY");
        //    intent.SetPackage("com.google.android.gms");
        //    StartActivity(intent);
        //};

        UIHelper.SetupToolBar(this, GetString(Resource.String.app_titlebar_title_default));

        FindViewById<TextView>(Resource.Id.infoTextView)!.TextFormatted = UIHelper.LoadHtmlAsset(this, "Welcome");

        Button sendButton = FindViewById<Button>(Resource.Id.sendButton)!;
        sendButton.Click += SendButton_Click;

        Button receiveButton = FindViewById<Button>(Resource.Id.receiveButton)!;
        receiveButton.Click += ReceiveButton_Click;

        FloatingActionButton floatingReceiveButton = FindViewById<FloatingActionButton>(Resource.Id.floating_receive_button)!;
        floatingReceiveButton.Click += ReceiveButton_Click;
    }

    private void ReceiveButton_Click(object? sender, EventArgs e)
    {
        StartActivity(new Intent(this, typeof(ReceiveActivity)));
    }

    const int FilePickCode = 0x1;
    private void SendButton_Click(object? sender, System.EventArgs e)
    {
        StartActivityForResult(
            new Intent(Intent.ActionOpenDocument)
                .SetType("*/*")
                .AddCategory(Intent.CategoryOpenable)
                .PutExtra(Intent.ExtraAllowMultiple, true),
            FilePickCode
        );
    }

    protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent? data)
    {
        if (resultCode != Result.Ok || data == null)
            return;

        if (requestCode == FilePickCode)
        {
            Intent intent = new(this, typeof(SendActivity));

            var clipData = data.ClipData;
            if (data.Data != null)
            {
                intent.SetAction(Intent.ActionSend);
                intent.PutExtra(Intent.ExtraStream, data.Data);
            }
            else if (clipData != null)
            {
                intent.SetAction(Intent.ActionSendMultiple);

                List<IParcelable> uriList = new();
                for (int i = 0; i < clipData.ItemCount; i++)
                {
                    var item = clipData.GetItemAt(i);
                    if (item?.Uri == null)
                        continue;

                    uriList.Add(item.Uri);
                }

                intent.PutParcelableArrayListExtra(Intent.ExtraStream, uriList);
            }
            else
                return;

            StartActivity(intent);
        }
    }

    public override bool OnCreateOptionsMenu(IMenu? menu)
        => UIHelper.OnCreateOptionsMenu(this, menu);

    public override bool OnOptionsItemSelected(IMenuItem item)
        => UIHelper.OnOptionsItemSelected(this, item);
}
