using Android.Bluetooth;
using Android.Content;
using Android.OS;
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
using Nearby_Sharing_Windows.Settings;
using ShortDev.Android.UI;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;

namespace Nearby_Sharing_Windows;

[IntentFilter([Intent.ActionProcessText], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataMimeType = "text/plain", Label = "@string/share_text")]
[IntentFilter([Intent.ActionSend, Intent.ActionSendMultiple], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataMimeType = "*/*", Label = "@string/share_file")]
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataMimeType = "text/plain", Label = "@string/share_url")]
[Activity(Label = "@string/app_name", Exported = true, Theme = "@style/AppTheme.TranslucentOverlay", ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class SendActivity : AppCompatActivity
{
    NearShareSender NearShareSender = null!;

    BottomSheetDialog _dialog = null!;
    RecyclerView DeviceDiscoveryListView = null!;
    TextView StatusTextView = null!;
    Button cancelButton = null!;
    Button readyButton = null!;

    ILogger<SendActivity> _logger = null!;
    ILoggerFactory _loggerFactory = null!;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        SetContentView(new CoordinatorLayout(this)
        {
            LayoutParameters = new(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        });
        Window!.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);

        _dialog = new(this);
        _dialog.SetContentView(Resource.Layout.activity_share);
        _dialog.DismissWithAnimation = true;
        _dialog.Behavior.State = BottomSheetBehavior.StateExpanded;
        _dialog.Behavior.FitToContents = true;
        _dialog.Behavior.Draggable = false;
        _dialog.Behavior.AddBottomSheetCallback(new FinishActivityBottomSheetCallback(this));
        _dialog.Window?.ClearFlags(WindowManagerFlags.DimBehind);

        _dialog.FindViewById<ViewGroup>(Resource.Id.rootLayout)!.EnableLayoutTransition();

        StatusTextView = _dialog.FindViewById<TextView>(Resource.Id.statusTextView)!;

        cancelButton = _dialog.FindViewById<Button>(Resource.Id.cancel_button)!;
        cancelButton.Click += CancelButton_Click;

        readyButton = _dialog.FindViewById<Button>(Resource.Id.readyButton)!;
        readyButton.Click += (s, e) => _dialog.Cancel();

        DeviceDiscoveryListView = _dialog.FindViewById<RecyclerView>(Resource.Id.deviceSelector)!;
        DeviceDiscoveryListView.SetLayoutManager(new LinearLayoutManager(this, (int)Orientation.Horizontal, reverseLayout: false));
        var adapterDescriptor = new AdapterDescriptor<CdpDevice>(
            Resource.Layout.item_device,
            (view, device) =>
            {
                view.FindViewById<ImageView>(Resource.Id.deviceTypeImageView)!.SetImageResource(
                    device.Type.IsMobile() ? Resource.Drawable.ic_fluent_phone_24_regular : Resource.Drawable.ic_fluent_desktop_24_regular
                );
                view.FindViewById<ImageView>(Resource.Id.transportTypeImageView)!.SetImageResource(device.Endpoint.TransportType switch
                {
                    CdpTransportType.Tcp => Resource.Drawable.ic_fluent_wifi_1_20_regular,
                    CdpTransportType.Rfcomm => Resource.Drawable.ic_fluent_bluetooth_20_regular,
                    CdpTransportType.WifiDirect => Resource.Drawable.ic_fluent_live_20_regular,
                    _ => Resource.Drawable.ic_fluent_question_circle_20_regular
                });
                view.FindViewById<TextView>(Resource.Id.deviceNameTextView)!.Text = device.Name;
                view.Click += (s, e) => SendData(device);
            }
        );
        DeviceDiscoveryListView.SetAdapter(adapterDescriptor.CreateRecyclerViewAdapter(RemoteSystems));

        _loggerFactory = ConnectedDevicesPlatform.CreateLoggerFactory(this.GetLogFilePattern());
        _logger = _loggerFactory.CreateLogger<SendActivity>();

        UIHelper.RequestSendPermissions(this);
    }

    public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
    {
        _logger.RequestPermissionResult(requestCode, permissions, grantResults);
        try
        {
            await Task.Run(InitializePlatform);
            _dialog.Show();
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
        var service = (BluetoothManager)GetSystemService(BluetoothService)!;
        var adapter = service.Adapter!;

        _cdp = new(new()
        {
            Type = DeviceType.Android,
            Name = SettingsFragment.GetDeviceName(this, adapter),
            OemModelName = Build.Model ?? string.Empty,
            OemManufacturerName = Build.Manufacturer ?? string.Empty,
            DeviceCertificate = ConnectedDevicesPlatform.CreateDeviceCertificate(CdpEncryptionParams.Default)
        }, _loggerFactory);

        AndroidBluetoothHandler bluetoothHandler = new(adapter, PhysicalAddress.None);
        _cdp.AddTransport<BluetoothTransport>(new(bluetoothHandler));

        AndroidNetworkHandler networkHandler = new(this);
        _cdp.AddTransport<NetworkTransport>(new(networkHandler));

        _cdp.DeviceDiscovered += Platform_DeviceDiscovered;
        _cdp.Discover(_discoverCancellationTokenSource.Token);

        NearShareSender = new NearShareSender(_cdp);
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
                _dialog.FindViewById<View>(Resource.Id.emptyDeviceListView)!.Visibility = ViewStates.Gone;
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

    readonly CancellationTokenSource _fileSendCancellationTokenSource = new();
    private async void SendData(CdpDevice remoteSystem)
    {
        _discoverCancellationTokenSource.Cancel();

        _dialog.FindViewById<View>(Resource.Id.selectDeviceLayout)!.Visibility = ViewStates.Gone;

        var sendingDataLayout = _dialog.FindViewById<View>(Resource.Id.sendingDataLayout)!;
        sendingDataLayout.Visibility = ViewStates.Visible;

        sendingDataLayout.FindViewById<ImageView>(Resource.Id.deviceTypeImageView)!.SetImageResource(
            remoteSystem.Type.IsMobile() ? Resource.Drawable.ic_fluent_phone_24_regular : Resource.Drawable.ic_fluent_desktop_24_regular
        );
        sendingDataLayout.FindViewById<ImageView>(Resource.Id.transportTypeImageView)!.SetImageResource(remoteSystem.Endpoint.TransportType switch
        {
            CdpTransportType.Tcp => Resource.Drawable.ic_fluent_wifi_1_20_regular,
            CdpTransportType.Rfcomm => Resource.Drawable.ic_fluent_bluetooth_20_regular,
            CdpTransportType.WifiDirect => Resource.Drawable.ic_fluent_live_20_regular,
            _ => Resource.Drawable.ic_fluent_question_circle_20_regular
        });

        var deviceNameTextView = sendingDataLayout.FindViewById<TextView>(Resource.Id.deviceNameTextView)!;
        var progressIndicator = sendingDataLayout.FindViewById<CircularProgressIndicator>(Resource.Id.sendProgressIndicator)!;
        progressIndicator.SetProgressCompat(0, animated: false);

        deviceNameTextView.Text = remoteSystem.Name;
        StatusTextView.Text = GetString(Resource.String.wait_for_acceptance);
        try
        {
            if (remoteSystem.Endpoint.TransportType == CdpTransportType.Rfcomm &&
                _cdp.TryGetTransport<BluetoothTransport>()?.Handler.IsEnabled == false)
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
                transferPromise = NearShareSender.SendFilesAsync(
                    remoteSystem,
                    files,
                    progress,
                    _fileSendCancellationTokenSource.Token
                );
            }
            else if (uri != null)
            {
                transferPromise = NearShareSender.SendUriAsync(
                    remoteSystem,
                    uri
                );
            }

            if (progress != null)
            {
                cancelButton.Visibility = ViewStates.Visible;

                progressIndicator.SetIndicatorColor([
                    this.GetColorAttr(Resource.Attribute.colorPrimary)
                ]);
                progressIndicator.Indeterminate = true;
                progress.ProgressChanged += (s, args) =>
                {
                    RunOnUiThread(() =>
                    {
#if !DEBUG
                        try
                        {
#endif
                        progressIndicator.Indeterminate = false;
                        progressIndicator.Max = (int)args.TotalBytesToSend;
                        progressIndicator.SetProgressCompat((int)args.BytesSent, animated: true);

                        if (args.TotalFilesToSend != 0 && args.TotalBytesToSend != 0)
                        {
                            StatusTextView.Text = this.Localize(
                                Resource.String.sending_template,
                                args.TotalFilesToSend
                            );
                        }
#if !DEBUG
                        }
                        catch { }
#endif
                    });
                };
            }

            if (transferPromise != null)
                await transferPromise;

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
        catch (TaskCanceledException)
        {
            // Ignore cancellation
            StatusTextView.Text = this.Localize(Resource.String.status_cancelled);
        }
        catch (Exception ex)
        {
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
        }
    }

    (IReadOnlyList<CdpFileProvider>? files, Uri? uri) ParseIntentAsync()
    {
        ArgumentNullException.ThrowIfNull(Intent);

        if (Intent.Action == Intent.ActionProcessText && OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            return (
                files: new[] { SendText(Intent.GetStringExtra(Intent.ExtraProcessText)) },
                null
            );
        }

        if (Intent.Action == Intent.ActionSendMultiple)
        {
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
                files: new[] { SendText(text) },
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
            _fileSendCancellationTokenSource.Cancel();
        }
        catch { }
    }

    public override void Finish()
    {
        base.Finish();

        _cdp?.Dispose();
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