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
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using SystemDebug = System.Diagnostics.Debug;

namespace NearShare;

[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", ConfigurationChanges = UIHelper.ConfigChangesFlags, LaunchMode = LaunchMode.SingleTask)]
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
            _notifications.CreateAdapter(
                Resource.Layout.item_transfer_notification,
                view => new TransferNotificationViewHolder(view)
                {
                    OnRemove = _notifications.Remove,
                    RunOnUiThread = RunOnUiThread
                }
            )
        );

        FindViewById<Button>(Resource.Id.openFAQButton)!.Click += (s, e) => UIHelper.OpenFAQ(this);

        _loggerFactory = CdpUtils.CreateLoggerFactory(this);
        _logger = _loggerFactory.CreateLogger<ReceiveActivity>();

        UIHelper.RequestReceivePermissions(this);
    }

    sealed class TransferNotificationViewHolder : ViewHolder<TransferToken>
    {
        readonly Context _context;
        readonly Button _acceptButton, _openButton, _cancelButton;
        readonly TextView _fileNameTextView, _detailsTextView;
        readonly CircularProgressIndicator _loadingProgressIndicator;
        public TransferNotificationViewHolder(View view) : base(view)
        {
            _context = view.Context!;

            _acceptButton = view.FindViewById<Button>(Resource.Id.acceptButton)!;
            _acceptButton.Click += AcceptButton_Click;

            _openButton = view.FindViewById<Button>(Resource.Id.openButton)!;
            _openButton.Click += OpenButton_Click;

            _cancelButton = view.FindViewById<Button>(Resource.Id.cancelButton)!;
            _cancelButton.Click += CancelButton_Click;

            _fileNameTextView = view.FindViewById<TextView>(Resource.Id.fileNameTextView)!;
            _detailsTextView = view.FindViewById<TextView>(Resource.Id.detailsTextView)!;
            _loadingProgressIndicator = view.FindViewById<CircularProgressIndicator>(Resource.Id.loadingProgressIndicator)!;
        }

        TransferToken? _transfer;
        public override void Bind(int index, TransferToken transfer)
        {
            _transfer = transfer;

            switch (transfer)
            {
                case UriTransferToken uriTransfer:
                    _fileNameTextView.Text = uriTransfer.Uri;
                    _detailsTextView.Text = uriTransfer.DeviceName;

                    _acceptButton.Visibility = ViewStates.Gone;
                    _openButton.Visibility = ViewStates.Visible;
                    break;

                case FileTransferToken fileTransfer:
                    _fileNameTextView.Text = string.Join(", ", fileTransfer.Files.Select(x => x.Name));
                    _detailsTextView.Text = $"{fileTransfer.DeviceName} • {FileTransferToken.FormatFileSize(fileTransfer.TotalBytes)}";
                    _loadingProgressIndicator.Indeterminate = true;
                    UpdateUI(fileTransfer);

                    fileTransfer.Progress += OnProgress;
                    break;
            }
        }

        public override void Recycle()
        {
            if (_transfer is not FileTransferToken fileTransfer)
                return;

            fileTransfer.Progress -= OnProgress;
        }

        private void AcceptButton_Click(object? sender, EventArgs e)
        {
            if (_transfer is not FileTransferToken fileTransfer)
                return;

            try
            {
                var streams = fileTransfer.ToFrozenDictionary(x => x.Id, file => (Stream)_context.ContentResolver!.CreateMediaStoreStream(file.Name).stream);
                fileTransfer.Accept(streams ?? throw new UnreachableException("Could not generated streams to accept"));

                fileTransfer.Finished += () =>
                {
                    // ToDo: Delete failed transfers
                };
            }
            catch (Exception ex)
            {
                new MaterialAlertDialogBuilder(_context)
                    .SetTitle(ex.GetType().Name)!
                    .SetMessage(ex.Message)!
                    .Show();
            }

            UpdateUI(fileTransfer);
        }

        public required Action<Action> RunOnUiThread { get; init; }
        void OnProgress(NearShareProgress progress)
        {
            if (_transfer is not FileTransferToken fileTransfer)
                return;

            RunOnUiThread(() =>
            {
                _loadingProgressIndicator.Indeterminate = false;

                int progressInt = progress.TotalBytes == 0 ? 0 : Math.Min((int)(progress.TransferedBytes * 100 / progress.TotalBytes), 100);
                if (OperatingSystem.IsAndroidVersionAtLeast(24))
                    _loadingProgressIndicator.SetProgress(progressInt, animate: true);
                else
                    _loadingProgressIndicator.Progress = progressInt;

                UpdateUI(fileTransfer);
            });
        }

        public required Func<TransferToken, bool> OnRemove { get; init; }
        private void CancelButton_Click(object? sender, EventArgs e)
        {
            if (_transfer is null)
                return;

            if (_transfer is FileTransferToken fileTransfer)
                fileTransfer.Cancel();

            OnRemove(_transfer);
        }

        private void OpenButton_Click(object? sender, EventArgs e)
        {
            switch (_transfer)
            {
                case UriTransferToken uriTransfer:
                    UIHelper.DisplayWebSite(_context, uriTransfer.Uri);
                    break;

                case FileTransferToken:
                    _context.ViewDownloads();
                    // ToDo: View single file
                    //if (fileTransfer.Files.Count == 1)
                    break;
            }
        }

        void UpdateUI(FileTransferToken fileTransfer)
        {
            _acceptButton.Visibility = !fileTransfer.IsTransferComplete && !fileTransfer.IsAccepted ? ViewStates.Visible : ViewStates.Gone;
            _loadingProgressIndicator.Visibility = !fileTransfer.IsTransferComplete && fileTransfer.IsAccepted ? ViewStates.Visible : ViewStates.Gone;
            _openButton.Visibility = fileTransfer.IsTransferComplete ? ViewStates.Visible : ViewStates.Gone;
        }
    }

    CancellationTokenSource? _cancellationTokenSource;
    ConnectedDevicesPlatform? _cdp;
    void InitializeCDP()
    {
        if (btAddress == null)
            throw new NullReferenceException(nameof(btAddress));

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new();

        SystemDebug.Assert(_cdp == null);

        _cdp = CdpUtils.Create(this, _loggerFactory);

        _cdp.Listen(_cancellationTokenSource.Token);
        _cdp.Advertise(_cancellationTokenSource.Token);

        NearShareReceiver.Register(_cdp);
        NearShareReceiver.ReceivedUri += OnTransfer;
        NearShareReceiver.FileTransfer += OnTransfer;

        FindViewById<TextView>(Resource.Id.deviceInfoTextView)!.Text = this.Localize(
            Resource.String.visible_as_template,
            _cdp.DeviceInfo.Name
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
        _cdp?.Dispose();
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