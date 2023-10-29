using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Snackbar;
using Nearby_Sharing_Windows.Settings;
using ShortDev.Android.UI;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;

namespace Nearby_Sharing_Windows;

[IntentFilter(new[] { Intent.ActionProcessText }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "text/plain", Label = "@string/share_text")]
[IntentFilter(new[] { Intent.ActionSend, Intent.ActionSendMultiple }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "*/*", Label = "@string/share_file")]
[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "text/plain", Label = "@string/share_url")]
[Activity(Label = "@string/app_name", Exported = true, Theme = "@style/AppTheme.TranslucentOverlay", ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class SendActivity : AppCompatActivity, View.IOnApplyWindowInsetsListener, ICdpPlatformHandler
{
    [AllowNull] NearShareSender NearShareSender;

    [AllowNull] RecyclerView DeviceDiscoveryListView;
    [AllowNull] TextView StatusTextView;
    [AllowNull] FrameLayout bottomSheetFrame;
    [AllowNull] Button cancelButton;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_share);

        StatusTextView = FindViewById<TextView>(Resource.Id.statusTextView)!;
        bottomSheetFrame = FindViewById<FrameLayout>(Resource.Id.standard_bottom_sheet)!;
        cancelButton = FindViewById<Button>(Resource.Id.cancel_button)!;

        DeviceDiscoveryListView = FindViewById<RecyclerView>(Resource.Id.deviceSelector)!;
        DeviceDiscoveryListView.SetLayoutManager(new GridLayoutManager(this, 2));
        adapterDescriptor = new AdapterDescriptor<CdpDevice>(
            Resource.Layout.item_device,
            (view, device) =>
            {
                view.FindViewById<ImageView>(Resource.Id.deviceTypeImageView)?.SetImageResource(
                    device.Type.IsMobile() ? Resource.Drawable.ic_fluent_phone_24_regular : Resource.Drawable.ic_fluent_desktop_24_regular
                );
                view.FindViewById<ImageView>(Resource.Id.transportTypeImageView)?.SetImageResource(
                    device.Endpoint.TransportType == CdpTransportType.Tcp ? Resource.Drawable.ic_fluent_plug_connected_20_regular : Resource.Drawable.ic_fluent_live_20_regular
                );
                view.FindViewById<TextView>(Resource.Id.deviceNameTextView)!.Text = device.Name;
                view.Click += (s, e) => SendData(device);
            }
        );

        Window!.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
            Window.InsetsController!.SetSystemBarsAppearance(
                (int)WindowInsetsControllerAppearance.LightNavigationBars,
                (int)WindowInsetsControllerAppearance.LightNavigationBars
            );
        else
            Window!.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LightNavigationBar;
        Window!.DecorView.SetOnApplyWindowInsetsListener(this);

        cancelButton.Click += CancelButton_Click;

        UIHelper.RequestSendPermissions(this);
    }

    #region UI
    public WindowInsets OnApplyWindowInsets(View? v, WindowInsets? windowInsets)
    {
        ArgumentNullException.ThrowIfNull(windowInsets);

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var insets = windowInsets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
            bottomSheetFrame.SetPadding(
                insets.Left,
                /* insets.Top */ 0,
                insets.Right,
                insets.Bottom
            );
        }
        else
        {
            bottomSheetFrame.SetPadding(
                windowInsets.StableInsetLeft,
                /* insets.Top */ 0,
                windowInsets.StableInsetRight,
                windowInsets.StableInsetBottom
            );
        }
        return windowInsets;
    }
    #endregion

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
    {
        if (grantResults.Contains(Android.Content.PM.Permission.Denied))
        {
            Snackbar.Make(Window!.DecorView, GetString(Resource.String.send_missing_permissions), Snackbar.LengthLong).Show();
        }

        RunOnUiThread(() => InitializePlatform());
    }

    #region Initialization
    readonly CancellationTokenSource _discoverCancellationTokenSource = new();
    [AllowNull] ConnectedDevicesPlatform Platform { get; set; }
    void InitializePlatform()
    {
        var service = (BluetoothManager)GetSystemService(BluetoothService)!;
        var adapter = service.Adapter!;

        Platform = new(new()
        {
            Type = DeviceType.Android,
            Name = SettingsFragment.GetDeviceName(this, adapter),
            OemModelName = Build.Model ?? string.Empty,
            OemManufacturerName = Build.Manufacturer ?? string.Empty,
            DeviceCertificate = ConnectedDevicesPlatform.CreateDeviceCertificate(CdpEncryptionParams.Default),
            LoggerFactory = ConnectedDevicesPlatform.CreateLoggerFactory(msg => System.Diagnostics.Debug.Print(msg), this.GetLogFilePattern())
        });

        AndroidBluetoothHandler bluetoothHandler = new(this, adapter, PhysicalAddress.None);
        Platform.AddTransport<BluetoothTransport>(new(bluetoothHandler));

        AndroidNetworkHandler networkHandler = new(this, this);
        Platform.AddTransport<NetworkTransport>(new(networkHandler));

        Platform.DeviceDiscovered += Platform_DeviceDiscovered;
        Platform.Discover(_discoverCancellationTokenSource.Token);

        NearShareSender = new NearShareSender(Platform);
    }

    readonly List<CdpDevice> RemoteSystems = new();
    private void Platform_DeviceDiscovered(ICdpTransport sender, CdpDevice device, BLeBeacon advertisement)
    {
        if (!RemoteSystems.Contains(device))
        {
            RemoteSystems.Add(device);
            RunOnUiThread(() => UpdateUI());
        }
    }
    #endregion

    #region RemoteSystemUI
    [MaybeNull] AdapterDescriptor<CdpDevice> adapterDescriptor;
    private void UpdateUI()
    {
        FindViewById<View>(Resource.Id.emptyDeviceListView)!.Visibility = RemoteSystems.Count == 0 ? ViewStates.Visible : ViewStates.Gone;
        DeviceDiscoveryListView.SetAdapter(adapterDescriptor!.CreateRecyclerViewAdapter(RemoteSystems));
    }
    #endregion



    readonly CancellationTokenSource _fileSendCancellationTokenSource = new();
    private async void SendData(CdpDevice remoteSystem)
    {
        _discoverCancellationTokenSource.Cancel();

        StatusTextView.Text = GetString(Resource.String.wait_for_acceptance);
        try
        {
            try
            {
                Task? fileTransferOperation = null;
                Progress<NearShareProgress> progress = new();
                Task? uriTransferOperation = null;

                var (files, uri) = ParseIntentAsync();
                if (files != null)
                {
                    fileTransferOperation = NearShareSender.SendFilesAsync(
                        remoteSystem,
                        files,
                        progress,
                        _fileSendCancellationTokenSource.Token
                    );
                }
                else if (uri != null)
                {
                    uriTransferOperation = NearShareSender.SendUriAsync(
                        remoteSystem,
                        uri
                    );
                }

                FindViewById<View>(Resource.Id.selectDeviceLayout)!.Visibility = ViewStates.Gone;
                FindViewById<View>(Resource.Id.sendingDataLayout)!.Visibility = ViewStates.Visible;

                FindViewById<TextView>(Resource.Id.currentDeviceTextView)!.Text = remoteSystem.Name;

                CircularProgressIndicator progressIndicator = FindViewById<CircularProgressIndicator>(Resource.Id.sendProgressIndicator)!;
                if (fileTransferOperation != null)
                {
                    progress.ProgressChanged += (s, args) =>
                    {
                        RunOnUiThread(() =>
                        {
#if !DEBUG
                            try
                            {
#endif
                            progressIndicator.Max = (int)args.TotalBytesToSend;
                            progressIndicator.Progress = (int)args.BytesSent;

                            if (args.TotalFilesToSend != 0 && args.TotalBytesToSend != 0)
                            {
                                StatusTextView.Text = this.Localize(
                                    Resource.String.sending_template,
                                    args.FilesSent, args.TotalFilesToSend,
                                    Math.Round((decimal)args.BytesSent / args.TotalBytesToSend * 100)
                                );
                                OnRequestAccepted();
                            }
#if !DEBUG
                            }
                            catch { }
#endif
                        });
                    };
                    cancelButton.Enabled = true;
                    await fileTransferOperation;
                    cancelButton.Enabled = false;
                    fileTransferOperation = null;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(uriTransferOperation != null, "\"uriTransferOperation\" is null!");

                    OnRequestAccepted();
                    await uriTransferOperation;
                }

                FindViewById(Resource.Id.doneIndicatorImageView)!.Visibility = ViewStates.Visible;
            }
            finally
            {
                FinishAsync();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Make(Window!.DecorView, this.Localize(Resource.String.generic_error_template, ex.Message), Snackbar.LengthLong).Show();
        }
    }

    (IReadOnlyList<CdpFileProvider>? files, Uri? uri) ParseIntentAsync()
    {
        ArgumentNullException.ThrowIfNull(Intent);

        if (Intent.Action == Intent.ActionProcessText)
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
                    files: new[] { ContentResolver!.CreateNearShareFileFromContentUri(fileUri) },
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

    void OnRequestAccepted()
    {
        FindViewById(Resource.Id.loadingProgressIndicator)!.Visibility = ViewStates.Gone;
        FindViewById(Resource.Id.waitForAcceptanceView)!.Visibility = ViewStates.Gone;
        FindViewById(Resource.Id.progressUILayout)!.Visibility = ViewStates.Visible;
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        try
        {
            _fileSendCancellationTokenSource.Cancel();
        }
        catch { }
    }

    #region Finish
    async void FinishAsync(int delayMs = 1500)
    {
        await Task.Delay(delayMs);
        Finish();
    }

    public override void OnBackPressed()
        => Finish();
    public override void Finish()
    {
        Platform?.Dispose();
        base.Finish();
    }
    #endregion
}