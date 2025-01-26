using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Dialog;
using Google.Android.Material.ProgressIndicator;
using Microsoft.Extensions.Logging;
using NearShare.Droid;
using NearShare.Utils;
using ShortDev.Android.UI;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Microsoft.ConnectedDevices.Transports.Network;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace NearShare;

[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class ReceiveActivity : AppCompatActivity
{
    RecyclerView notificationsRecyclerView = null!;
    readonly ObservableCollection<TransferToken> _notifications = [];

    PhysicalAddress? btAddress = null;

    ILogger<ReceiveActivity> _logger = null!;
    ILoggerFactory _loggerFactory = null!;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (ReceiveSetupActivity.IsSetupRequired(this) || !ReceiveSetupActivity.TryGetBtAddress(this, out btAddress))
        {
            StartActivity(new Intent(this, typeof(ReceiveSetupActivity)));

            Finish();
            return;
        }

        SetContentView(Resource.Layout.activity_receive);

        UIHelper.SetupToolBar(this, GetString(Resource.String.app_titlebar_title_receive));

        notificationsRecyclerView = FindViewById<RecyclerView>(Resource.Id.notificationsRecyclerView)!;
        notificationsRecyclerView.SetLayoutManager(new LinearLayoutManager(this));
        notificationsRecyclerView.SetAdapter(
            new AdapterDescriptor<TransferToken>(
                Resource.Layout.item_transfer_notification,
                OnInflateNotification
            ).CreateRecyclerViewAdapter(_notifications)
        );

        FindViewById<Button>(Resource.Id.openFAQButton)!.Click += (s, e) => UIHelper.OpenFAQ(this);

        _loggerFactory = CdpUtils.CreateLoggerFactory(this);
        _logger = _loggerFactory.CreateLogger<ReceiveActivity>();

        UIHelper.RequestReceivePermissions(this);
    }

    void OnInflateNotification(View view, TransferToken transfer)
    {
        var acceptButton = view.FindViewById<Button>(Resource.Id.acceptButton)!;
        var openButton = view.FindViewById<Button>(Resource.Id.openButton)!;
        var fileNameTextView = view.FindViewById<TextView>(Resource.Id.fileNameTextView)!;
        var detailsTextView = view.FindViewById<TextView>(Resource.Id.detailsTextView)!;
        var loadingProgressIndicator = view.FindViewById<CircularProgressIndicator>(Resource.Id.loadingProgressIndicator)!;

        view.FindViewById<Button>(Resource.Id.cancelButton)!.Click += (s, e) =>
        {
            if (transfer is FileTransferToken fileTransfer)
                fileTransfer.Cancel();

            _notifications.Remove(transfer);
        };

        if (transfer is UriTransferToken uriTranfer)
        {
            fileNameTextView.Text = uriTranfer.Uri;
            detailsTextView.Text = uriTranfer.DeviceName;

            acceptButton.Visibility = ViewStates.Gone;
            openButton.Visibility = ViewStates.Visible;

            openButton.Click += (s, e) => UIHelper.DisplayWebSite(this, uriTranfer.Uri);

            return;
        }

        if (transfer is not FileTransferToken fileTransfer)
            throw new UnreachableException();

        fileNameTextView.Text = string.Join(", ", fileTransfer.Files.Select(x => x.Name));
        detailsTextView.Text = $"{fileTransfer.DeviceName} • {FileTransferToken.FormatFileSize(fileTransfer.TotalBytes)}";

        acceptButton.Click += OnAccept;

        fileTransfer.Progress += progress => RunOnUiThread(() => OnProgress(progress));
        loadingProgressIndicator.Indeterminate = true;

        openButton.Click += (_, _) =>
        {
            this.ViewDownloads();

            // ToDo: View single file
            // if (fileTransfer.Files.Count == 1)
        };

        UpdateUI();

        void OnAccept(object? sender, EventArgs e)
        {
            try
            {
                var streams = fileTransfer.Select(file => ContentResolver!.CreateMediaStoreStream(file.Name).stream).ToArray();
                fileTransfer.Accept(streams);

                fileTransfer.Finished += () =>
                {
                    // ToDo: Delete failed transfers
                };
            }
            catch (Exception ex)
            {
                new MaterialAlertDialogBuilder(this)
                    .SetTitle(ex.GetType().Name)!
                    .SetMessage(ex.Message)!
                    .Show();
            }

            UpdateUI();
        }

        void OnProgress(NearShareProgress progress)
        {
            loadingProgressIndicator.Indeterminate = false;

            int progressInt = progress.TotalBytes == 0 ? 0 : Math.Min((int)(progress.TransferedBytes * 100 / progress.TotalBytes), 100);
            if (OperatingSystem.IsAndroidVersionAtLeast(24))
                loadingProgressIndicator.SetProgress(progressInt, animate: true);
            else
                loadingProgressIndicator.Progress = progressInt;

            UpdateUI();
        }

        void UpdateUI()
        {
            acceptButton.Visibility = !fileTransfer.IsTransferComplete && !fileTransfer.IsAccepted ? ViewStates.Visible : ViewStates.Gone;
            loadingProgressIndicator.Visibility = !fileTransfer.IsTransferComplete && fileTransfer.IsAccepted ? ViewStates.Visible : ViewStates.Gone;
            openButton.Visibility = fileTransfer.IsTransferComplete ? ViewStates.Visible : ViewStates.Gone;
        }
    }

    CancellationTokenSource? _cancellationTokenSource;
    async void InitializeCDP()
    {
        if (btAddress == null)
            throw new NullReferenceException(nameof(btAddress));

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new();

        var service = await CdpService.EnsureRunning(this);
        var platform = service.Platform;

        platform.Listen(_cancellationTokenSource.Token);
        platform.Advertise(_cancellationTokenSource.Token);

        NearShareReceiver.Register(platform);
        NearShareReceiver.ReceivedUri += OnTransfer;
        NearShareReceiver.FileTransfer += OnTransfer;

        FindViewById<TextView>(Resource.Id.deviceInfoTextView)!.Text = this.Localize(
            Resource.String.visible_as_template,
            platform.DeviceInfo.Name
        );
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        _logger.RequestPermissionResult(requestCode, permissions, grantResults);

        InitializeCDP();
    }

    public override bool OnCreateOptionsMenu(IMenu? menu)
        => UIHelper.OnCreateOptionsMenu(this, menu);

    public override bool OnOptionsItemSelected(IMenuItem item)
        => UIHelper.OnOptionsItemSelected(this, item);

    public override void Finish()
    {
        _cancellationTokenSource?.Cancel();
        NearShareReceiver.Unregister();
        base.Finish();
    }

    void OnTransfer(TransferToken transfer)
        => RunOnUiThread(() => _notifications.Add(transfer));
}

static class Extensions
{
    public static EndpointInfo ToCdp(this BluetoothDevice @this)
        => new(
            CdpTransportType.Rfcomm,
            @this.Address ?? throw new InvalidDataException("Empty address"),
            Constants.RfcommServiceId
        );

    public static CdpSocket ToCdp(this BluetoothSocket @this)
        => new()
        {
            InputStream = @this.InputStream ?? throw new NullReferenceException(),
            OutputStream = @this.OutputStream ?? throw new NullReferenceException(),
            Endpoint = @this.RemoteDevice!.ToCdp(),
            Close = @this.Close
        };
}