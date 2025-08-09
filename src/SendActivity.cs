using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Color;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.SideSheet;
using Microsoft.Extensions.Logging;
using NearShare.Droid;
using NearShare.Utils;
using NearShare.ViewModels;
using ShortDev.Android.UI;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NearShare;

[IntentFilter([Intent.ActionProcessText], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataMimeType = "text/plain", Label = "@string/app_name")]
[IntentFilter([Intent.ActionSend, Intent.ActionSendMultiple], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataMimeType = "*/*")]
[Activity(Label = "@string/app_name", Exported = true, Theme = "@style/AppTheme.DialogActivity", ConfigurationChanges = UIHelper.ConfigChangesFlags, LaunchMode = LaunchMode.Multiple)]
public sealed partial class SendActivity : AppCompatActivity
{
    NearShareSender _nearShareSender = null!;

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

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
            Window?.NavigationBarContrastEnforced = false;

        SetContentView(Resource.Layout.activity_share_dialog);
        var sidesheetElement = FindViewById(Resource.Id.side_sheet)!;
        SideSheetBehavior.From(sidesheetElement).Expand();
        sidesheetElement.Click += (s, e) => { }; // Catch clicks so we don't get closed!
        FindViewById(Resource.Id.touch_outside)!.Click += (s, e) => Finish();

        FindViewById<ViewGroup>(Resource.Id.rootLayout)!.EnableLayoutTransition();

        StatusTextView = FindViewById<TextView>(Resource.Id.statusTextView)!;

        cancelButton = FindViewById<Button>(Resource.Id.cancel_button)!;
        cancelButton.Click += CancelButton_Click;

        readyButton = FindViewById<Button>(Resource.Id.readyButton)!;
        readyButton.Click += (s, e) => Finish();

        DeviceDiscoveryListView = FindViewById<RecyclerView>(Resource.Id.deviceSelector)!;
        DeviceDiscoveryListView.SetLayoutManager(new GridLayoutManager(this, spanCount: 2, (int)Orientation.Vertical, reverseLayout: false));
        DeviceDiscoveryListView.SetAdapter(
            RemoteSystems.CreateAdapter(Resource.Layout.item_device, view => new RemoteSystemViewHolder(view) { Click = SendData })
        );

        _emptyDeviceListView = FindViewById<View>(Resource.Id.emptyDeviceListView)!;

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

    public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        _logger.RequestPermissionResult(requestCode, permissions, grantResults);
        try
        {
            await Task.Run(InitializePlatform);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);

            if (!Lifecycle.CurrentState.IsAtLeast(AndroidX.Lifecycle.Lifecycle.State.Started!))
                return;

            this.ShowErrorDialog(ex);
        }
    }

    #region Initialization
    readonly CancellationTokenSource _discoverCancellationTokenSource = new();
    ConnectedDevicesPlatform _cdp = null!;
    void InitializePlatform()
    {
        _cdp = CdpUtils.Create(this, _loggerFactory);
        _nearShareSender = new NearShareSender(_cdp);

        _cdp.DeviceDiscovered += Platform_DeviceDiscovered;
        _cdp.Discover(_discoverCancellationTokenSource.Token);
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

    SendDataViewModel? _currentTransfer;
    private void SendData(CdpDevice remoteSystem)
    {
        _discoverCancellationTokenSource.Cancel();

        _currentTransfer = ParseIntentAsync() switch
        {
            ({ } files, null) => SendDataViewModel.SendFiles(_nearShareSender, remoteSystem, files),
            (null, { } uri) => SendDataViewModel.SendUri(_nearShareSender, remoteSystem, uri),
            _ => null
        };

        if (_currentTransfer is null)
            return; // ToDo: Feedback?!

        FindViewById<View>(Resource.Id.selectDeviceLayout)!.Visibility = ViewStates.Gone;

        var sendingDataLayout = FindViewById<View>(Resource.Id.sendingDataLayout)!;
        sendingDataLayout.Visibility = ViewStates.Visible;

        sendingDataLayout.FindViewById<ImageView>(Resource.Id.deviceTypeImageView)!.SetImageResource(
            _currentTransfer.IsMobile ? Resource.Drawable.ic_fluent_phone_24_regular : Resource.Drawable.ic_fluent_desktop_24_regular
        );

        var transportTypeImage = sendingDataLayout.FindViewById<ImageView>(Resource.Id.transportTypeImageView)!;
        transportTypeImage.SetImageResource(GetTransportIcon(_currentTransfer.CurrentTransportType));

        var deviceNameTextView = sendingDataLayout.FindViewById<TextView>(Resource.Id.deviceNameTextView)!;
        var progressIndicator = sendingDataLayout.FindViewById<CircularProgressIndicator>(Resource.Id.sendProgressIndicator)!;
        progressIndicator.Visibility = ViewStates.Visible;
        progressIndicator.SetProgressCompat(0, animated: false);
        progressIndicator.SetIndicatorColor([
            this.GetColorAttr(Resource.Attribute.colorPrimary)
        ]);
        progressIndicator.Indeterminate = true;

        deviceNameTextView.Text = remoteSystem.Name;
        StatusTextView.Text = GetString(Resource.String.wait_for_acceptance);

        cancelButton.Visibility = ViewStates.Visible;

        ViewModelObserver.Observe(this, _currentTransfer, OnChanged);
        _currentTransfer.Start();

        void OnChanged(SendDataViewModel viewModel, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SendDataViewModel.TotalBytes):
                    progressIndicator.Indeterminate = false;
                    progressIndicator.Max = viewModel.TotalBytes;
                    break;

                case nameof(SendDataViewModel.ProgressBytes):
                    progressIndicator.SetProgressCompat(viewModel.ProgressBytes, animated: true);
                    break;

                case nameof(SendDataViewModel.TotalFiles):
                    var progressTemplate = GetString(Resource.String.sending_template);
                    StatusTextView.Text = string.Format(
                        progressTemplate,
                        viewModel.TotalFiles
                    );
                    break;

                case nameof(SendDataViewModel.CurrentTransportType):
                    transportTypeImage.SetImageResource(GetTransportIcon(viewModel.CurrentTransportType));
                    break;

                case nameof(SendDataViewModel.State):
                    const SendDataViewModel.States AllFinished = (SendDataViewModel.States)42;
                    switch (viewModel.State)
                    {
                        case SendDataViewModel.States.InProgress:
                            break;

                        case SendDataViewModel.States.Succeeded:
                            progressIndicator.SetIndicatorColor([
                                MaterialColors.HarmonizeWithPrimary(this,
                                    ContextCompat.GetColor(this, Resource.Color.status_success)
                                )
                            ]);

                            StatusTextView.Text = this.Localize(Resource.String.status_done);
                            StatusTextView.PerformHapticFeedback(
                                OperatingSystem.IsAndroidVersionAtLeast(30) ? FeedbackConstants.Confirm : FeedbackConstants.LongPress,
                                FeedbackFlags.IgnoreGlobalSetting
                            );
                            this.PlaySound(Resource.Raw.ding);
                            goto case AllFinished;

                        case SendDataViewModel.States.Cancelled:
                            StatusTextView.Text = this.Localize(Resource.String.status_cancelled);
                            goto case AllFinished;

                        case SendDataViewModel.States.Failed:
                            progressIndicator.SetIndicatorColor([
                                this.GetColorAttr(Resource.Attribute.colorError)
                            ]);
                            if (OperatingSystem.IsAndroidVersionAtLeast(30))
                                StatusTextView.PerformHapticFeedback(FeedbackConstants.Reject, FeedbackFlags.IgnoreGlobalSetting);

                            if (viewModel.Error is not { } ex)
                            {
                                StatusTextView.Text = this.Localize(Resource.String.generic_error_template);
                            }
                            else
                            {
                                StatusTextView.Text = ex.GetType().Name;
                                this.ShowErrorDialog(ex);
                            }

                            goto case AllFinished;

                        case AllFinished:
                            cancelButton.Visibility = ViewStates.Gone;
                            readyButton.Visibility = ViewStates.Visible;

                            progressIndicator.Indeterminate = false;
                            progressIndicator.Progress = progressIndicator.Max;

                            _currentTransfer = null;
                            break;
                    }
                    break;
            }
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
            _currentTransfer?.Cancel();
        }
        catch { }
    }

    protected override void OnStop()
    {
        base.OnStop();

        if (_currentTransfer is null)
            Finish();
    }

    public override void Finish()
    {
        _discoverCancellationTokenSource.Cancel();
        try
        {
            _currentTransfer?.Cancel();
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