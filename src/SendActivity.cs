using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Color;
using Google.Android.Material.ProgressIndicator;
using Microsoft.Extensions.Logging;
using NearShare.Droid;
using NearShare.Utils;
using ShortDev.Android.UI;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Collections.ObjectModel;
using OperationCanceledException = System.OperationCanceledException;

namespace NearShare;

[IntentFilter([Intent.ActionProcessText], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataMimeType = "text/plain", Label = "@string/app_name")]
[IntentFilter([Intent.ActionSend, Intent.ActionSendMultiple], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataMimeType = "*/*")]
[Activity(Label = "@string/app_name", Exported = true, Theme = "@style/AppTheme.TranslucentOverlay", ConfigurationChanges = UIHelper.ConfigChangesFlags, LaunchMode = LaunchMode.SingleTask)]
public sealed class SendActivity : AppCompatActivity
{
    NearShareSender _nearShareSender = null!;

    BottomSheetDialog _dialog = null!;
    RecyclerView DeviceDiscoveryListView = null!;
    TextView StatusTextView = null!;
    Button cancelButton = null!;
    Button readyButton = null!;
    View _emptyDeviceListView = null!;

    ILogger<SendActivity> _logger = null!;
    ILoggerFactory _loggerFactory = null!;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        this.EnableEdgeToEdge();
        base.OnCreate(savedInstanceState);

        SetContentView(new CoordinatorLayout(this)
        {
            LayoutParameters = new(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        });

        _dialog = new(this);
        _dialog.SetContentView(Resource.Layout.activity_share);
        _dialog.DismissWithAnimation = true;
        _dialog.Behavior.State = BottomSheetBehavior.StateExpanded;
        _dialog.Behavior.FitToContents = true;
        _dialog.Behavior.Draggable = false;
        _dialog.Behavior.AddBottomSheetCallback(new FinishActivityBottomSheetCallback(this));
        _dialog.Window?.ClearFlags(WindowManagerFlags.DimBehind);
        _dialog.Show();

        _dialog.FindViewById<ViewGroup>(Resource.Id.rootLayout)!.EnableLayoutTransition();

        StatusTextView = _dialog.FindViewById<TextView>(Resource.Id.statusTextView)!;

        cancelButton = _dialog.FindViewById<Button>(Resource.Id.cancel_button)!;
        cancelButton.Click += CancelButton_Click;

        readyButton = _dialog.FindViewById<Button>(Resource.Id.readyButton)!;
        readyButton.Click += (s, e) => _dialog.Cancel();

        DeviceDiscoveryListView = _dialog.FindViewById<RecyclerView>(Resource.Id.deviceSelector)!;
        DeviceDiscoveryListView.SetLayoutManager(new LinearLayoutManager(this, (int)Orientation.Horizontal, reverseLayout: false));
        DeviceDiscoveryListView.SetAdapter(
            RemoteSystems.CreateAdapter(Resource.Layout.item_device, view => new RemoteSystemViewHolder(view) { Click = SendData })
        );

        _emptyDeviceListView = _dialog.FindViewById<View>(Resource.Id.emptyDeviceListView)!;

        _loggerFactory = CdpUtils.CreateLoggerFactory(this);
        _logger = _loggerFactory.CreateLogger<SendActivity>();

        UIHelper.RequestSendPermissions(this);
    }

    sealed class RemoteSystemViewHolder : ViewHolder<CdpDevice>
    {
        readonly ImageView _deviceType, _transportType;
        readonly TextView _deviceName;
        public RemoteSystemViewHolder(View view) : base(view)
        {
            _deviceType = view.FindViewById<ImageView>(Resource.Id.deviceTypeImageView)!;
            _transportType = view.FindViewById<ImageView>(Resource.Id.transportTypeImageView)!;
            _deviceName = view.FindViewById<TextView>(Resource.Id.deviceNameTextView)!;

            view.Click += OnClick;
        }

        CdpDevice? _remoteSystem;
        public override void Bind(int index, CdpDevice device)
        {
            _remoteSystem = device;

            _deviceType.SetImageResource(
                device.Type.IsMobile() ? Resource.Drawable.ic_fluent_phone_24_regular : Resource.Drawable.ic_fluent_desktop_24_regular
            );
            _transportType.SetImageResource(GetTransportIcon(device.Endpoint.TransportType));
            _deviceName.Text = device.Name;
        }

        public required Action<CdpDevice> Click { get; init; }
        private void OnClick(object? sender, EventArgs e)
        {
            if (_remoteSystem is null)
                return;

            Click(_remoteSystem);
        }
    }

    public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
    {
        _logger.RequestPermissionResult(requestCode, permissions, grantResults);
        try
        {
            await Task.Run(InitializePlatform).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.ShowErrorDialog(ex);
        }
    }

    #region Initialization
    readonly CancellationTokenSource _discoverCancellationTokenSource = new();
    ConnectedDevicesPlatform _cdp = null!;
    void InitializePlatform()
    {
        _cdp = CdpUtils.Create(this, _loggerFactory);

        _cdp.DeviceDiscovered += Platform_DeviceDiscovered;
        _cdp.Discover(_discoverCancellationTokenSource.Token);

        _nearShareSender = new NearShareSender(_cdp);
    }

    readonly ObservableCollection<CdpDevice> RemoteSystems = [];
    private void Platform_DeviceDiscovered(ICdpTransport sender, CdpDevice device)
    {
        RunOnUiThread(() =>
        {
            lock (RemoteSystems)
            {
                var newIndex = FindIndex(RemoteSystems, device);
                var oldIndex = RemoteSystems.IndexOf(device);
                if (oldIndex != -1)
                {
                    // ToDo: Move if signal strength changed
                    // Currently might flicker
                    // RemoteSystems.Move(oldIndex, newIndex);
                    return;
                }

                RemoteSystems.Insert(newIndex, device);
                _emptyDeviceListView.Visibility = ViewStates.Gone;
                this.PlaySound(Resource.Raw.pop);
            }
        });

        static int FindIndex(IReadOnlyList<CdpDevice> devices, CdpDevice newDevice)
        {
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i].Rssi > newDevice.Rssi)
                    continue;

                return i;
            }
            return 0;
        }
    }
    #endregion

    readonly CancellationTokenSource _transferCancellation = new();
    private async void SendData(CdpDevice remoteSystem)
    {
        _discoverCancellationTokenSource.Cancel();

        _dialog.FindViewById<View>(Resource.Id.selectDeviceLayout)!.Visibility = ViewStates.Gone;

        var sendingDataLayout = _dialog.FindViewById<View>(Resource.Id.sendingDataLayout)!;
        sendingDataLayout.Visibility = ViewStates.Visible;

        sendingDataLayout.FindViewById<ImageView>(Resource.Id.deviceTypeImageView)!.SetImageResource(
            remoteSystem.Type.IsMobile() ? Resource.Drawable.ic_fluent_phone_24_regular : Resource.Drawable.ic_fluent_desktop_24_regular
        );

        var transportTypeImage = sendingDataLayout.FindViewById<ImageView>(Resource.Id.transportTypeImageView)!;
        transportTypeImage.SetImageResource(GetTransportIcon(remoteSystem.Endpoint.TransportType));
        _nearShareSender.TransportUpgraded += OnTransportUpgrade;

        var deviceNameTextView = sendingDataLayout.FindViewById<TextView>(Resource.Id.deviceNameTextView)!;
        var progressIndicator = sendingDataLayout.FindViewById<CircularProgressIndicator>(Resource.Id.sendProgressIndicator)!;
        progressIndicator.SetProgressCompat(0, animated: false);

        deviceNameTextView.Text = remoteSystem.Name;
        StatusTextView.Text = GetString(Resource.String.wait_for_acceptance);
        try
        {
            if (remoteSystem.Endpoint.TransportType == CdpTransportType.Rfcomm &&
                _cdp.TryGetTransport(CdpTransportType.Rfcomm)?.IsEnabled == false)
            {
                StartActivityForResult(new Intent(BluetoothAdapter.ActionRequestEnable), 42);
                throw new TaskCanceledException("Bluetooth is disabled");
            }

            Progress<NearShareProgress>? progress = null;

            Task? transferPromise = null;
            var (files, uri) = ParseIntentAsync();
            if (files != null)
            {
                progress = new();
                transferPromise = _nearShareSender.SendFilesAsync(
                    remoteSystem,
                    files,
                    progress,
                    _transferCancellation.Token
                );
            }
            else if (uri != null)
            {
                transferPromise = _nearShareSender.SendUriAsync(
                    remoteSystem,
                    uri,
                    _transferCancellation.Token
                );
            }

            if (progress != null)
            {
                cancelButton.Visibility = ViewStates.Visible;

                progressIndicator.SetIndicatorColor([
                    this.GetColorAttr(Resource.Attribute.colorPrimary)
                ]);
                progressIndicator.Indeterminate = true;

                var progressTemplate = GetString(Resource.String.sending_template);
                progress.ProgressChanged += (s, args) =>
                {
                    RunOnUiThread(() =>
                    {
                        try
                        {
                            progressIndicator.Indeterminate = false;
                            progressIndicator.Max = (int)args.TotalBytes;
                            progressIndicator.SetProgressCompat((int)args.TransferedBytes, animated: true);

                            if (args.TotalFiles != 0 && args.TotalBytes != 0)
                            {
                                StatusTextView.Text = string.Format(
                                    progressTemplate,
                                    args.TotalFiles
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.Fail(ex.Message);
                        }
                    });
                };
            }

            if (transferPromise != null)
                await transferPromise;

            if (!Lifecycle.CurrentState.IsAtLeast(AndroidX.Lifecycle.Lifecycle.State.Started!))
                return;

            progressIndicator.SetIndicatorColor([
                MaterialColors.HarmonizeWithPrimary(this,
                    ContextCompat.GetColor(this, Resource.Color.status_success)
                )
            ]);

            StatusTextView.Text = this.Localize(Resource.String.status_done);
            StatusTextView.PerformHapticFeedback(
                OperatingSystem.IsAndroidVersionAtLeast(30) ? FeedbackConstants.Confirm : FeedbackConstants.LongPress
            , FeedbackFlags.IgnoreGlobalSetting);
            this.PlaySound(Resource.Raw.ding);
        }
        catch (OperationCanceledException)
        {
            if (!Lifecycle.CurrentState.IsAtLeast(AndroidX.Lifecycle.Lifecycle.State.Started!))
                return;

            // Ignore cancellation
            StatusTextView.Text = this.Localize(Resource.String.status_cancelled);
        }
        catch (Exception ex)
        {
            if (!Lifecycle.CurrentState.IsAtLeast(AndroidX.Lifecycle.Lifecycle.State.Started!))
                return;

            this.ShowErrorDialog(ex);

            progressIndicator.SetIndicatorColor([
                this.GetColorAttr(Resource.Attribute.colorError)
            ]);
            StatusTextView.Text = ex.GetType().Name;
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
                StatusTextView.PerformHapticFeedback(FeedbackConstants.Reject, FeedbackFlags.IgnoreGlobalSetting);
        }
        finally
        {
            cancelButton.Visibility = ViewStates.Gone;
            readyButton.Visibility = ViewStates.Visible;

            progressIndicator.Indeterminate = false;
            progressIndicator.Progress = progressIndicator.Max;

            _nearShareSender.TransportUpgraded -= OnTransportUpgrade;
        }

        void OnTransportUpgrade(object? sender, CdpTransportType transportType)
        {
            RunOnUiThread(() =>
                transportTypeImage.SetImageResource(GetTransportIcon(transportType))
            );
        }
    }

    (IReadOnlyList<CdpFileProvider>? files, Uri? uri) ParseIntentAsync()
    {
        ArgumentNullException.ThrowIfNull(Intent);

        if (Intent.Action == Intent.ActionProcessText && OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            return (
                files: [SendText(Intent.GetStringExtra(Intent.ExtraProcessText))],
                null
            );
        }

        if (Intent.Action == Intent.ActionSendMultiple)
        {
            if (Intent.HasExtra(Intent.ExtraText))
            {
                return (
                    files: (Intent.GetStringArrayListExtra(Intent.ExtraText) ?? throw new InvalidDataException("Could not get extra files from intent"))
                        .Select(SendText)
                        .ToArray(),
                    null
                );
            }

            return (
                files: (Intent.GetParcelableArrayListExtra<AndroidUri>(Intent.ExtraStream) ?? throw new InvalidDataException("Could not get extra files from intent"))
                    .Select(ContentResolver!.CreateNearShareFileFromContentUri)
                    .ToArray(),
                null
            );
        }

        if (Intent.Action == Intent.ActionSend)
        {
            if (Intent.HasExtra(Intent.ExtraStream))
            {
                AndroidUri fileUri = Intent.GetParcelableExtra<AndroidUri>(Intent.ExtraStream) ?? throw new InvalidDataException("Could not get ExtraStream");
                return (
                    files: [ContentResolver!.CreateNearShareFileFromContentUri(fileUri)],
                    null
                );
            }

            var text = Intent.GetStringExtra(Intent.ExtraText) ?? "";
            if (Uri.IsWellFormedUriString(text, UriKind.Absolute))
            {
                return (
                    null,
                    uri: new(text)
                );
            }

            return (
                files: [SendText(text)],
                null
            );
        }

        return (null, null);

        static CdpFileProvider SendText(string? text)
        {
            return CdpFileProvider.FromContent(
                $"Text-Transfer-{DateTime.Now:dd_MM_yyyy-HH_mm_ss}.txt",
                text ?? throw new NullReferenceException("Text was null")
            );
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        try
        {
            _transferCancellation.Cancel();
        }
        catch { }
    }

    public override void Finish()
    {
        _discoverCancellationTokenSource.Cancel();
        try
        {
            _transferCancellation.Cancel();
        }
        catch { }
        _cdp?.Dispose();

        base.Finish();
    }

    static int GetTransportIcon(CdpTransportType transportType)
    {
        return transportType switch
        {
            CdpTransportType.Tcp => Resource.Drawable.ic_fluent_wifi_1_20_regular,
            CdpTransportType.Rfcomm => Resource.Drawable.ic_fluent_bluetooth_20_regular,
            CdpTransportType.WifiDirect => Resource.Drawable.ic_fluent_live_20_regular,
            _ => Resource.Drawable.ic_fluent_question_circle_20_regular
        };
    }

    sealed class FinishActivityBottomSheetCallback(Activity activity) : BottomSheetBehavior.BottomSheetCallback
    {
        public override void OnSlide(View bottomSheet, float newState) { }

        public override void OnStateChanged(View p0, int p1)
        {
            if (p1 != BottomSheetBehavior.StateHidden)
                return;

            activity.Finish();
        }
    }
}