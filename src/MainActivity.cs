using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using Google.Android.Material.Card;
using Google.Android.Material.Dialog;
using Google.Android.Material.TextField;

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

        FindViewById<MaterialCardView>(Resource.Id.sendFileCard)!.Click += SendButton_Click;
        FindViewById<MaterialCardView>(Resource.Id.sendClipBoardCard)!.Click += SendClipBoard_Click;
        FindViewById<MaterialCardView>(Resource.Id.sendTextCard)!.Click += SendText_Click;

        FindViewById<MaterialCardView>(Resource.Id.enableBluetoothButton)!.Click += (_, _) => EnableBluetooth();
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

    private void SendClipBoard_Click(object? sender, EventArgs e)
    {
        try
        {
            var clipboard = (ClipboardManager?)GetSystemService(ClipboardService);
            if (clipboard is null)
                return;

            var clip = clipboard.PrimaryClip;
            if (clip is null)
                return;

            List<string> values = [];
            for (int i = 0; i < clip.ItemCount; i++)
            {
                var value = clip.GetItemAt(0)?.CoerceToText(this);
                if (value is not null)
                    values.Add(value);
            }

            if (values.Count == 0)
                return;

            Intent intent = new(this, typeof(SendActivity));
            intent.SetAction(Intent.ActionSendMultiple);
            intent.PutStringArrayListExtra(Intent.ExtraText, values);
            StartActivity(intent);
        }
        catch (Exception ex)
        {
            this.ShowErrorDialog(ex);
        }
    }

    private void SendText_Click(object? sender, EventArgs e)
    {
        try
        {
            TextInputEditText editText = new(this)
            {
                LayoutParameters = new(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
            };
            new MaterialAlertDialogBuilder(this)
                .SetTitle(Resource.String.share_text)!
                .SetView(editText)!
                .SetPositiveButton(Resource.String.generic_send, (s, e) =>
                {
                    Intent intent = new(this, typeof(SendActivity));
                    intent.SetAction(Intent.ActionSend);
                    intent.PutExtra(Intent.ExtraText, editText.Text);
                    StartActivity(intent);
                })
                .SetNegativeButton(Resource.String.generic_cancel, (s, e) => { })
                .Show();
        }
        catch (Exception ex)
        {
            this.ShowErrorDialog(ex);
        }
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

    private void EnableBluetooth()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
            ActivityCompat.RequestPermissions(this, [ManifestPermission.BluetoothConnect], EnableBluetoothCode);
        else
            StartActivity(new Intent(BluetoothAdapter.ActionRequestEnable));
    }

    const int EnableBluetoothCode = 0x2;
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
    {
        if (requestCode != EnableBluetoothCode || grantResults.Any(x => x != Android.Content.PM.Permission.Granted))
            return;

        StartActivity(new Intent(BluetoothAdapter.ActionRequestEnable));
    }
}
