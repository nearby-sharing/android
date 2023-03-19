using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Snackbar;
using ShortDev.Android.UI;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using AndroidUri = Android.Net.Uri;
using ManifestPermission = Android.Manifest.Permission;

namespace Nearby_Sharing_Windows;

[IntentFilter(new[] { Intent.ActionSend, Intent.ActionSendMultiple }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "*/*", Label = "Send file")]
[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "text/plain", Label = "Send url")]
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
                    "" == "Desktop" ? Resource.Drawable.ic_fluent_desktop_20_regular : Resource.Drawable.ic_fluent_phone_20_regular
                );
                view.FindViewById<TextView>(Resource.Id.deviceNameTextView)!.Text = device.Name;
                view.Click += (s, e) => SendData(device);
            }
        );

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
        {
            Window!.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
            Window!.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LightNavigationBar;
            Window!.DecorView.SetOnApplyWindowInsetsListener(this);
        }

        cancelButton.Click += CancelButton_Click;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            ActivityCompat.RequestPermissions(this, new[] {
                ManifestPermission.AccessFineLocation,
                ManifestPermission.AccessCoarseLocation,
                ManifestPermission.BluetoothScan,
                ManifestPermission.BluetoothConnect
            }, 0);
        }
        else
        {
            ActivityCompat.RequestPermissions(this, new[] {
                ManifestPermission.AccessFineLocation,
                ManifestPermission.AccessCoarseLocation
            }, 0);
        }
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
#pragma warning disable CS0618 // Type or member is obsolete
            bottomSheetFrame.SetPadding(
                windowInsets.StableInsetLeft,
                /* insets.Top */ 0,
                windowInsets.StableInsetRight,
                windowInsets.StableInsetBottom
            );
#pragma warning restore CS0618 // Type or member is obsolete
        }
        return windowInsets;
    }
    #endregion

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
    {
        if (grantResults.Contains(Android.Content.PM.Permission.Denied))
            Snackbar.Make(Window!.DecorView, "Error: Missing permission!", Snackbar.LengthLong).Show();
        else
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
            Name = adapter.Name ?? throw new NullReferenceException("Could not find device name"),
            OemModelName = Build.Model ?? string.Empty,
            OemManufacturerName = Build.Manufacturer ?? string.Empty,
            DeviceCertificate = ConnectedDevicesPlatform.CreateDeviceCertificate(CdpEncryptionParams.Default),
            LoggerFactory = ConnectedDevicesPlatform.CreateLoggerFactory(msg => System.Diagnostics.Debug.Print(msg))
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
    private void Platform_DeviceDiscovered(ICdpTransport sender, CdpDevice device, CdpAdvertisement advertisement)
    {
        if (!RemoteSystems.Contains(device))
        {
            RemoteSystems.Add(device);
            UpdateUI();
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

    async Task<CdpFileProvider> CreateNearShareFileFromContentUriAsync(AndroidUri contentUri)
    {
        var fileName = QueryContentName(ContentResolver!, contentUri);

        using var contentStream = ContentResolver!.OpenInputStream(contentUri) ?? throw new InvalidOperationException("Could not open input stream");
        var buffer = new byte[contentStream.Length];
        await contentStream.ReadAsync(buffer);

        return CdpFileProvider.FromBuffer(fileName, buffer);
    }

    static string QueryContentName(ContentResolver resolver, AndroidUri contentUri)
    {
        using var returnCursor = resolver.Query(contentUri, null, null, null, null) ?? throw new InvalidOperationException("Could not open content cursor");
        int nameIndex = returnCursor.GetColumnIndex(IOpenableColumns.DisplayName);
        returnCursor.MoveToFirst();
        return returnCursor.GetString(nameIndex) ?? throw new InvalidOperationException("Could not query content name");
    }

    readonly CancellationTokenSource _fileSendCancellationTokenSource = new();
    private async void SendData(CdpDevice remoteSystem)
    {
        _discoverCancellationTokenSource.Cancel();

        StatusTextView.Text = "Waiting for acceptance...";
        try
        {
            try
            {
                Task? fileTransferOperation = null;
                Progress<NearShareProgress> fileSendProgress = new();
                Task? uriTransferOperation = null;
                if (Intent?.Action == Intent.ActionSend)
                {
                    if (Intent.HasExtra(Intent.ExtraStream))
                    {
                        AndroidUri file = (Intent.GetParcelableExtra(Intent.ExtraStream) as AndroidUri)!;
                        fileTransferOperation = NearShareSender.SendFileAsync(
                            remoteSystem,
                            await CreateNearShareFileFromContentUriAsync(file),
                            fileSendProgress,
                            _fileSendCancellationTokenSource.Token
                        );
                    }
                    else
                    {
                        uriTransferOperation = NearShareSender.SendUriAsync(
                            remoteSystem,
                           new Uri(Intent.GetStringExtra(Intent.ExtraText)!)
                        );
                    }
                }
                else if (Intent?.Action == Intent.ActionSendMultiple)
                {
                    var files = Intent.GetParcelableArrayListExtra(Intent.ExtraStream)?.Cast<AndroidUri>() ?? throw new InvalidDataException("Could not get extra files from intent");
                    fileTransferOperation = NearShareSender.SendFilesAsync(
                        remoteSystem,
                        await Task.WhenAll(files.Select(CreateNearShareFileFromContentUriAsync)),
                        fileSendProgress,
                        _fileSendCancellationTokenSource.Token
                    );
                }

                FindViewById<View>(Resource.Id.selectDeviceLayout)!.Visibility = ViewStates.Gone;
                FindViewById<View>(Resource.Id.sendingDataLayout)!.Visibility = ViewStates.Visible;

                FindViewById<TextView>(Resource.Id.currentDeviceTextView)!.Text = remoteSystem.Name;

                CircularProgressIndicator progressIndicator = FindViewById<CircularProgressIndicator>(Resource.Id.sendProgressIndicator)!;
                if (fileTransferOperation != null)
                {
                    fileSendProgress.ProgressChanged += (s, args) =>
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
                                StatusTextView.Text = $"Sending ... {args.FilesSent}/{args.TotalFilesToSend} files ... {Math.Round((decimal)args.BytesSent / args.TotalBytesToSend * 100)}%";
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
            Snackbar.Make(Window!.DecorView, $"Error: {ex.Message}", Snackbar.LengthLong).Show();
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
        base.Finish();
    }
    #endregion
}