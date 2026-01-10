using Android.Bluetooth;
using Android.Content;
using Android.Views;
using AndroidX.Fragment.App;
using Google.Android.Material.Card;
using Google.Android.Material.Dialog;
using Google.Android.Material.TextField;
using NearShare.Utils;
using ShortDev.Android.Views;
using ShortDev.Android.Lifecycle;
using System.Security;

namespace NearShare;

internal sealed class HomeFragment : Fragment
{
    RequestPermissionsLauncher _requestPermissionsLauncher = null!;
    public override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _requestPermissionsLauncher = new(
            this,
            RequireActivity(),
            OperatingSystem.IsAndroidVersionAtLeast(31) ? [
                ManifestPermission.BluetoothConnect
            ] : Array.Empty<string>()
        );
    }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        => inflater.Inflate(Resource.Layout.fragment_home, container, false);

    ViewBindings _viewBindings = null!;
    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        _viewBindings = new(view);

        _viewBindings.SendButton.Click += SendButton_Click;

        _viewBindings.SendFileCard.Click += SendButton_Click;
        _viewBindings.SendClipBoardCard.Click += SendClipBoard_Click;
        _viewBindings.SendTextCard.Click += SendText_Click;

        _viewBindings.EnableBluetoothCard.Click += EnableBluetooth_Click;
        _viewBindings.SetupWindowsCard.Click += delegate { UIHelper.OpenSetup(RequireActivity()); };
        _viewBindings.OpenFaqCard.Click += delegate { UIHelper.OpenFAQ(RequireActivity()); };
        _viewBindings.OpenCrowdinCard.Click += delegate { UIHelper.OpenCrowdIn(RequireActivity()); };
    }

    #region Transfer Setup
    private void SendButton_Click(object? sender, EventArgs e)
    {
        StartActivityForResult(
            new Intent(Intent.ActionOpenDocument)
                .SetType("*/*")
                .AddCategory(Intent.CategoryOpenable)
                .PutExtra(Intent.ExtraAllowMultiple, true),
            MainActivity.FilePickCode
        );
    }

    private void SendClipBoard_Click(object? sender, EventArgs e)
    {
        var ctx = RequireContext();
        try
        {
            var clipboard = (ClipboardManager?)ctx.GetSystemService(Context.ClipboardService);
            if (clipboard is null)
                return;

            var clip = clipboard.PrimaryClip;
            if (clip is null)
                return;

            List<string> values = [];
            for (int i = 0; i < clip.ItemCount; i++)
            {
                var value = clip.GetItemAt(0)?.CoerceToText(ctx);
                if (value is not null)
                    values.Add(value);
            }

            if (values.Count == 0)
                return;

            Intent intent = new(ctx, typeof(SendActivity));
            intent.SetAction(Intent.ActionSendMultiple);
            intent.PutStringArrayListExtra(Intent.ExtraText, values);
            StartActivity(intent);
        }
        catch (Exception ex)
        {
            ctx.ShowErrorDialog(ex);
        }
    }

    private void SendText_Click(object? sender, EventArgs e)
    {
        var ctx = RequireContext();
        try
        {
            TextInputEditText editText = new(ctx)
            {
                LayoutParameters = new(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
            };
            new MaterialAlertDialogBuilder(ctx)
                .SetTitle(Resource.String.share_text)!
                .SetView(editText)!
                .SetPositiveButton(Resource.String.generic_send, (s, e) =>
                {
                    Intent intent = new(ctx, typeof(SendActivity));
                    intent.SetAction(Intent.ActionSend);
                    intent.PutExtra(Intent.ExtraText, editText.Text);
                    StartActivity(intent);
                })
                .SetNegativeButton(Resource.String.generic_cancel, (s, e) => { })
                .Show();
        }
        catch (Exception ex)
        {
            ctx.ShowErrorDialog(ex);
        }
    }
    #endregion

    private async void EnableBluetooth_Click(object? sender, EventArgs e)
    {
        var ctx = RequireContext();
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(31) && await _requestPermissionsLauncher.RequestAsync() is PermissionResult.Denied)
            {
                if (!Lifecycle.IsAtLeastStarted)
                    return;

                ctx.ShowErrorDialog(new SecurityException("Bluetooth permission denied"));
                return;
            }

            StartActivityForResult(new Intent(BluetoothAdapter.ActionRequestEnable), 0);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);

            if (!Lifecycle.IsAtLeastStarted)
                return;

            ctx.ShowErrorDialog(ex);
        }
    }

    sealed class ViewBindings(View view)
    {
        public Button SendButton { get; } = view.FindRequiredViewById<Button>(Resource.Id.sendButton);

        public MaterialCardView SendFileCard { get; } = view.FindRequiredViewById<MaterialCardView>(Resource.Id.sendFileCard);
        public MaterialCardView SendClipBoardCard { get; } = view.FindRequiredViewById<MaterialCardView>(Resource.Id.sendClipBoardCard);
        public MaterialCardView SendTextCard { get; } = view.FindRequiredViewById<MaterialCardView>(Resource.Id.sendTextCard);

        public MaterialCardView EnableBluetoothCard { get; } = view.FindRequiredViewById<MaterialCardView>(Resource.Id.enableBluetoothCard);
        public MaterialCardView SetupWindowsCard { get; } = view.FindRequiredViewById<MaterialCardView>(Resource.Id.setupWindowsCard);
        public MaterialCardView OpenFaqCard { get; } = view.FindRequiredViewById<MaterialCardView>(Resource.Id.openFaqCard);
        public MaterialCardView OpenCrowdinCard { get; } = view.FindRequiredViewById<MaterialCardView>(Resource.Id.openCrowdinCard);
    }
}
