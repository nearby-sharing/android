using Android.Bluetooth;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Views;
using Google.Android.Material.Card;
using Google.Android.Material.Dialog;
using Google.Android.Material.TextField;
using NearShare.Utils;
using ShortDev.Android.Lifecycle;
using ShortDev.Android.Views;
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

        (_viewBindings.WelcomeImageView.Drawable as IAnimatable)?.Start();
    }

    #region Transfer Setup
    private async void SendButton_Click(object? sender, EventArgs e)
    {
        var ctx = RequireContext();

        Intent? result;
        try
        {
            result = await DelegateActivity.Launch(
                ctx,
                new Intent(Intent.ActionOpenDocument)
                    .SetType("*/*")
                    .AddCategory(Intent.CategoryOpenable)
                    .PutExtra(Intent.ExtraAllowMultiple, true)
            );
        }
        catch
        {
            return;
        }
        if (result is null)
            return;

        await Lifecycle.WaitUntilResumed();

        Intent intent = new(ctx, typeof(SendActivity));
        if (result is { Data: { } data })
        {
            intent.SetAction(Intent.ActionSend);
            intent.PutExtra(Intent.ExtraStream, data);
        }
        else if (result is { ClipData: { } clipData })
        {
            intent.SetAction(Intent.ActionSendMultiple);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            intent.ClipData = clipData;
        }
        else
            return;

        StartActivity(intent);
    }

    private void SendClipBoard_Click(object? sender, EventArgs e)
    {
        var ctx = RequireContext();
        try
        {
            var clipboard = (ClipboardManager?)ctx.GetSystemService(Context.ClipboardService);
            if (clipboard?.PrimaryClip is not { ItemCount: > 0 } clip)
            {
                Toast.MakeText(ctx, ctx.Localize(Resource.String.empty_clipboard), ToastLength.Short)!.Show();
                return;
            }

            Intent intent = new(ctx, typeof(SendActivity));
            intent.SetAction(Intent.ActionSendMultiple);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            intent.ClipData = clip;
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

            StartActivity(new Intent(BluetoothAdapter.ActionRequestEnable));
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
        public ImageView WelcomeImageView { get; } = view.FindRequiredViewById<ImageView>(Resource.Id.welcomeImageView);

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
