using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using Google.Android.Material.Card;

namespace NearShare.Droid;

[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class MainActivity : AppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);

        UIHelper.SetupToolBar(this);

        Button sendButton = FindViewById<Button>(Resource.Id.sendButton)!;
        sendButton.Click += SendButton_Click;

        Button receiveButton = FindViewById<Button>(Resource.Id.receiveButton)!;
        receiveButton.Click += ReceiveButton_Click;

        FindViewById<MaterialCardView>(Resource.Id.enableBluetoothButton)!.Click += (_, _) => StartActivity(new Intent(BluetoothAdapter.ActionRequestEnable));
        FindViewById<MaterialCardView>(Resource.Id.setupWindowButton)!.Click += (_, _) => UIHelper.OpenSetup(this);
        FindViewById<MaterialCardView>(Resource.Id.openFAQButton)!.Click += (_, _) => UIHelper.OpenFAQ(this);
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

                List<IParcelable> uriList = [];
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
