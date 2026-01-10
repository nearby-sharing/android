using Android.Bluetooth;
using Android.Content;
using Android.Views;
using AndroidX.Navigation;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Dialog;
using Google.Android.Material.ProgressIndicator;
using Microsoft.Extensions.Logging;
using NearShare.Utils;
using ShortDev.Android.Lifecycle;
using ShortDev.Android.UI;
using ShortDev.Android.Views;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using SystemDebug = System.Diagnostics.Debug;

namespace NearShare.Receive;

public sealed class ReceiveFragment : Fragment
{
    readonly SynchronizationContext _syncContext = SynchronizationContext.Current ?? throw new InvalidOperationException("No synchronization context");
    readonly ObservableCollection<TransferToken> _notifications = [];

    PhysicalAddress? btAddress = null;

    RequestPermissionsLauncher _requestPermissionsLauncher = null!;
    IntentResultListener _intentResultListener = null!;

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        => inflater.Inflate(Resource.Layout.fragment_receive, container, false);

    ILogger<ReceiveFragment> _logger = null!;
    ILoggerFactory _loggerFactory = null!;
    ViewBindings _viewBindings = null!;
    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        _viewBindings = new(view);

        var ctx = RequireContext();
        if (ReceiveSetupFragment.IsSetupRequired(ctx) || !ReceiveSetupFragment.TryGetBtAddress(ctx, out btAddress))
        {
            this.NavController.Navigate(Routes.ReceiveSetup, NavOptions.Create(builder =>
            {
                builder.InvokePopUpTo(Routes.Receive, options => options.Inclusive = true);
            }));
            return;
        }

        _viewBindings.NotificationsRecyclerView.SetLayoutManager(new LinearLayoutManager(ctx));
        _viewBindings.NotificationsRecyclerView.SetAdapter(
            _notifications.CreateAdapter(
                Resource.Layout.item_transfer_notification,
                view => new TransferNotificationViewHolder(view)
                {
                    OnRemove = _notifications.Remove,
                    RunOnUiThread = RunOnUiThread
                }
            )
        );

        _viewBindings.OpenFaqButton.Click += (s, e) => UIHelper.OpenFAQ(ctx);

        _loggerFactory = CdpUtils.CreateLoggerFactory(ctx);
        _logger = _loggerFactory.CreateLogger<ReceiveFragment>();

        _requestPermissionsLauncher = new(this, RequireActivity(), UIHelper.ReceivePermissions);
        _intentResultListener = new(this);

        InitializePlatformAsync();
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

        readonly ConditionalWeakTable<FileTransferToken, FrozenDictionary<uint, AndroidUri>> _uris = [];
        private void AcceptButton_Click(object? sender, EventArgs e)
        {
            if (_transfer is not FileTransferToken fileTransfer)
                return;

            try
            {
                var streams = fileTransfer.ToFrozenDictionary(x => x.Id, file => _context.ContentResolver!.CreateMediaStoreStream(file.Name))
                    ?? throw new UnreachableException("Could not generated streams to accept");

                fileTransfer.Accept(streams.ToFrozenDictionary(x => x.Key, x => (Stream)x.Value.stream));
                _uris.AddOrUpdate(fileTransfer, streams.ToFrozenDictionary(x => x.Key, x => x.Value.uri));

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
            {
                try
                {
                    fileTransfer.Cancel();
                }
                catch { }
            }

            OnRemove(_transfer);
        }

        private void OpenButton_Click(object? sender, EventArgs e)
        {
            switch (_transfer)
            {
                case UriTransferToken uriTransfer:
                    UIHelper.DisplayWebSite(_context, uriTransfer.Uri);
                    break;

                case FileTransferToken { Files: [var singleFile] } fileTransfer:
                    try
                    {
                        if (!_uris.TryGetValue(fileTransfer, out var lookup))
                            throw new UnreachableException("No entry in weak-table");

                        Intent viewFileIntent = new(Intent.ActionView);
                        viewFileIntent.SetData(lookup[singleFile.Id]);
                        viewFileIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
                        viewFileIntent.AddFlags(ActivityFlags.ClearTop);
                        _context.StartActivity(viewFileIntent);
                    }
                    catch (Exception ex)
                    {
                        SentrySdk.CaptureException(ex);
                        _context.ViewDownloads();
                    }
                    break;

                case FileTransferToken:
                    _context.ViewDownloads();
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

    async void InitializePlatformAsync()
    {
        if (await _requestPermissionsLauncher.RequestAsync() is PermissionResult.Denied(var denied))
        {
            if (!Lifecycle.IsAtLeastStarted)
                return;

            RequireContext().ShowErrorDialog(new UnauthorizedAccessException($"Required permissions were not granted:{Environment.NewLine}{string.Join(Environment.NewLine, denied)}"));
            return;
        }

        try
        {
            await _intentResultListener.LaunchAsync(new Intent(BluetoothAdapter.ActionRequestEnable));

            if (!Lifecycle.IsAtLeastStarted)
                return;

            await Task.Run(InitializePlatform);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);

            if (!this.IsAtLeastStarted)
                return;

            Context?.ShowErrorDialog(ex);
        }
    }

    CancellationTokenSource? _cancellationTokenSource;
    ConnectedDevicesPlatform? _cdp;
    void InitializePlatform()
    {
        if (btAddress == null)
            throw new InvalidOperationException("No bluetooth address");

        if (_cdp is not null)
            return;

        var ctx = RequireContext();

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new();

        SystemDebug.Assert(_cdp == null);

        _cdp = CdpUtils.Create(ctx, _loggerFactory);

        _cdp.Listen(_cancellationTokenSource.Token);
        _cdp.Advertise(_cancellationTokenSource.Token);

        NearShareReceiver.Register(_cdp);
        NearShareReceiver.ReceivedUri += OnTransfer;
        NearShareReceiver.FileTransfer += OnTransfer;

        _viewBindings.DeviceInfoTextView.Text = ctx.Localize(
            Resource.String.visible_as_template,
            _cdp.DeviceInfo.Name
        );
    }

    // ToDo: Fix cancellation on finish
    //public override void Finish()
    //{
    //    _cancellationTokenSource?.Cancel();
    //    _cdp?.Dispose();
    //    NearShareReceiver.Unregister();
    //    base.Finish();
    //}

    void OnTransfer(TransferToken transfer)
        => RunOnUiThread(() => _notifications.Add(transfer));

    void RunOnUiThread(Action action) => _syncContext.Post(static action => ((Action)action!)(), action);

    sealed class ViewBindings(View view)
    {
        public TextView DeviceInfoTextView { get; } = view.FindRequiredViewById<TextView>(Resource.Id.deviceInfoTextView);
        public Button OpenFaqButton { get; } = view.FindRequiredViewById<Button>(Resource.Id.openFaqButton);
        public RecyclerView NotificationsRecyclerView { get; } = view.FindRequiredViewById<RecyclerView>(Resource.Id.notificationsRecyclerView);
    }
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