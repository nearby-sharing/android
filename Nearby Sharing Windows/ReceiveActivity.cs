using Android.Bluetooth;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.ProgressIndicator;
using Nearby_Sharing_Windows.Settings;
using ShortDev.Android.UI;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;
using ShortDev.Microsoft.ConnectedDevices.Platforms.Network;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using SystemDebug = System.Diagnostics.Debug;

namespace Nearby_Sharing_Windows;

[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class ReceiveActivity : AppCompatActivity, INearSharePlatformHandler
{
    BluetoothAdapter? _btAdapter;

    [AllowNull] AdapterDescriptor<TransferToken> adapterDescriptor;
    [AllowNull] RecyclerView notificationsRecyclerView;
    readonly List<TransferToken> _notifications = new();

    PhysicalAddress? btAddress = null;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (ReceiveSetupActivity.IsSetupRequired(this) || !ReceiveSetupActivity.TryGetBtAddress(this, out btAddress) || btAddress == null)
        {
            StartActivity(new Android.Content.Intent(this, typeof(ReceiveSetupActivity)));

            Finish();
            return;
        }

        SetContentView(Resource.Layout.activity_receive);

        UIHelper.RequestReceivePermissions(this);
        UIHelper.SetupToolBar(this, GetString(Resource.String.app_titlebar_title_receive));

        notificationsRecyclerView = FindViewById<RecyclerView>(Resource.Id.notificationsRecyclerView)!;
        notificationsRecyclerView.SetLayoutManager(new LinearLayoutManager(this));

        FindViewById<Button>(Resource.Id.openFAQButton)!.Click += (s, e) => UIHelper.OpenFAQ(this);

        adapterDescriptor = new(
            Resource.Layout.item_transfer_notification,
            (view, transfer) =>
            {
                var acceptButton = view.FindViewById<Button>(Resource.Id.acceptButton)!;
                var openButton = view.FindViewById<Button>(Resource.Id.openButton)!;
                var fileNameTextView = view.FindViewById<TextView>(Resource.Id.fileNameTextView)!;
                var detailsTextView = view.FindViewById<TextView>(Resource.Id.detailsTextView)!;

                if (transfer is FileTransferToken fileTransfer)
                {
                    fileNameTextView.Text = string.Join(", ", fileTransfer.Files.Select(x => x.Name));
                    detailsTextView.Text = $"{fileTransfer.DeviceName} • {FileTransferToken.FormatFileSize(fileTransfer.TotalBytesToSend)}";

                    var loadingProgressIndicator = view.FindViewById<CircularProgressIndicator>(Resource.Id.loadingProgressIndicator)!;
                    void onCompleted()
                    {
                        acceptButton.Visibility = ViewStates.Gone;
                        loadingProgressIndicator.Visibility = ViewStates.Gone;
                        openButton.Visibility = ViewStates.Visible;
                        openButton.Click += (_, _) => this.ViewDownloads();
                    }
                    if (fileTransfer.IsTransferComplete)
                        onCompleted();
                    else
                    {
                        void onAccept()
                        {
                            if (!fileTransfer.IsAccepted)
                                fileTransfer.Accept(CreateFiles(fileTransfer));

                            acceptButton.Visibility = ViewStates.Gone;
                            loadingProgressIndicator.Visibility = ViewStates.Visible;

                            loadingProgressIndicator.Progress = 0;
                            void onProgress(NearShareProgress progress, bool animate)
                            {
                                loadingProgressIndicator.Indeterminate = false;

                                int progressInt = Math.Min((int)(progress.BytesSent * 100 / progress.TotalBytesToSend), 100);
                                if (OperatingSystem.IsAndroidVersionAtLeast(24))
                                    loadingProgressIndicator.SetProgress(progressInt, animate);
                                else
                                    loadingProgressIndicator.Progress = progressInt;

                                if (fileTransfer.IsTransferComplete)
                                    onCompleted();
                            }
                            fileTransfer.Progress += progress => RunOnUiThread(() => onProgress(progress, animate: true));
                            loadingProgressIndicator.Indeterminate = true;
                        }
                        if (fileTransfer.IsAccepted)
                            onAccept();
                        else
                            acceptButton.SetOnClickListener(new DelegateClickListener((s, e) => onAccept()));
                    }
                }
                else if (transfer is UriTransferToken uriTranfer)
                {
                    fileNameTextView.Text = uriTranfer.Uri;
                    detailsTextView.Text = uriTranfer.DeviceName;

                    acceptButton.Visibility = ViewStates.Gone;
                    openButton.Visibility = ViewStates.Visible;

                    openButton.SetOnClickListener(new DelegateClickListener((s, e) => UIHelper.DisplayWebSite(this, uriTranfer.Uri)));
                }

                view.FindViewById<Button>(Resource.Id.cancelButton)!.SetOnClickListener(new DelegateClickListener((s, e) =>
                {
                    _notifications.Remove(transfer);
                    UpdateUI();

                    if (transfer is FileTransferToken fileTransfer)
                        fileTransfer.Cancel();
                }));
            }
        );
    }

    CancellationTokenSource? _cancellationTokenSource;
    ConnectedDevicesPlatform? _cdp;
    void InitializeCDP()
    {
        if (btAddress == null)
            throw new NullReferenceException(nameof(btAddress));

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new();

        var service = (BluetoothManager)GetSystemService(BluetoothService)!;
        _btAdapter = service.Adapter!;

        var deviceName = SettingsFragment.GetDeviceName(this, _btAdapter);

        SystemDebug.Assert(_cdp == null);

        _cdp = new(new()
        {
            Type = DeviceType.Android,
            Name = deviceName,
            OemModelName = Build.Model ?? string.Empty,
            OemManufacturerName = Build.Manufacturer ?? string.Empty,
            DeviceCertificate = ConnectedDevicesPlatform.CreateDeviceCertificate(CdpEncryptionParams.Default),
            LoggerFactory = ConnectedDevicesPlatform.CreateLoggerFactory(this.GetLogFilePattern())
        });

        IBluetoothHandler bluetoothHandler = new AndroidBluetoothHandler(_btAdapter, btAddress);
        _cdp.AddTransport<BluetoothTransport>(new(bluetoothHandler));

        INetworkHandler networkHandler = new AndroidNetworkHandler(this);
        _cdp.AddTransport<NetworkTransport>(new(networkHandler));

        _cdp.Listen(_cancellationTokenSource.Token);
        _cdp.Advertise(_cancellationTokenSource.Token);

        NearShareReceiver.Start(_cdp, this);

        FindViewById<TextView>(Resource.Id.deviceInfoTextView)!.Text = this.Localize(
            Resource.String.visible_as_template,
            $"\"{deviceName}\".\n" +
            $"Address: {btAddress.ToStringFormatted()}\n" +
            $"IP-Address: {networkHandler.TryGetLocalIp()?.ToString() ?? "null"}"
        );
    }

    IReadOnlyList<Stream> CreateFiles(FileTransferToken token)
        => token.Select(file => this.CreateDownloadFile(file.Name)).ToArray();

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        if (grantResults.Contains(Permission.Denied))
        {
            Toast.MakeText(this, this.Localize(Resource.String.receive_missing_permissions), ToastLength.Long)!.Show();
        }

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
        NearShareReceiver.Stop();
        base.Finish();
    }

    void UpdateUI()
    {
        RunOnUiThread(() =>
        {
            notificationsRecyclerView.SetAdapter(adapterDescriptor.CreateRecyclerViewAdapter(_notifications));
        });
    }

    public void OnReceivedUri(UriTransferToken transfer)
    {
        _notifications.Add(transfer);
        UpdateUI();
    }

    public void OnFileTransfer(FileTransferToken transfer)
    {
        _notifications.Add(transfer);
        UpdateUI();
    }
}

static class Extensions
{
    public static CdpDevice ToCdp(this BluetoothDevice @this)
        => new(
            @this.Name ?? throw new InvalidDataException("Empty name"),
            DeviceType.Invalid,
            new(
                CdpTransportType.Rfcomm,
                @this.Address ?? throw new InvalidDataException("Empty address"),
                Constants.RfcommServiceId
            )
        );

    public static CdpSocket ToCdp(this BluetoothSocket @this)
        => new()
        {
            TransportType = CdpTransportType.Rfcomm,
            InputStream = @this.InputStream ?? throw new NullReferenceException(),
            OutputStream = @this.OutputStream ?? throw new NullReferenceException(),
            RemoteDevice = @this.RemoteDevice!.ToCdp(),
            Close = @this.Close
        };
}