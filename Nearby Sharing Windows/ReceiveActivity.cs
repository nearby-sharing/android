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

    [AllowNull] TextView debugLogTextView;

    [AllowNull] AdapterDescriptor<TransferToken> adapterDescriptor;
    [AllowNull] RecyclerView notificationsRecyclerView;
    readonly List<TransferToken> _notifications = new();

    PhysicalAddress? btAddress = null;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        SentryHelper.EnsureInitialized();

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
                    fileNameTextView.Text = string.Join(", ", fileTransfer.FileNames);
                    detailsTextView.Text = $"{fileTransfer.DeviceName} • {FileTransferToken.FormatFileSize(fileTransfer.TotalBytesToSend)}";

                    var loadingProgressIndicator = view.FindViewById<CircularProgressIndicator>(Resource.Id.loadingProgressIndicator)!;
                    void onCompleted()
                    {
                        acceptButton.Visibility = ViewStates.Gone;
                        loadingProgressIndicator.Visibility = ViewStates.Gone;
                        openButton.Visibility = ViewStates.Visible;
                        openButton.SetOnClickListener(new DelegateClickListener((s, e) =>
                        {
                            var firstFilePath = GetFilePath(fileTransfer.FileNames[0]);
                            if (fileTransfer.FileNames.Count == 1)
                                UIHelper.OpenFile(this, firstFilePath);

                            // ToDo: UIHelper.OpenDirectory(this, Path.GetDirectoryName(firstFilePath));
                        }));
                    }
                    if (fileTransfer.IsTransferComplete)
                        onCompleted();
                    else
                    {
                        void onAccept()
                        {
                            if (!fileTransfer.IsAccepted)
                                fileTransfer.Accept(fileTransfer.FileNames.Select(CreateFile).ToArray());

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
                            fileTransfer.SetProgressListener(progress => RunOnUiThread(() => onProgress(progress, animate: true)));
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
        FindViewById<TextView>(Resource.Id.deviceInfoTextView)!.Text = this.Localize(
            Resource.String.visible_as_template,
            $"\"{deviceName}\".\n" +
            $"Address: {btAddress.ToStringFormatted()}\n" +
            $"IP-Address: {AndroidNetworkHandler.GetLocalIp(this)}");
        debugLogTextView = FindViewById<TextView>(Resource.Id.debugLogTextView)!;
        debugLogTextView.SetTextIsSelectable(true);

        SystemDebug.Assert(_cdp == null);

        _cdp = new(new()
        {
            Type = DeviceType.Android,
            Name = deviceName,
            OemModelName = Build.Model ?? string.Empty,
            OemManufacturerName = Build.Manufacturer ?? string.Empty,
            DeviceCertificate = ConnectedDevicesPlatform.CreateDeviceCertificate(CdpEncryptionParams.Default),
            LoggerFactory = ConnectedDevicesPlatform.CreateLoggerFactory(Log)
        });

        AndroidBluetoothHandler bluetoothHandler = new(this, _btAdapter, btAddress);
        _cdp.AddTransport<BluetoothTransport>(new(bluetoothHandler));

        AndroidNetworkHandler networkHandler = new(this, this);
        _cdp.AddTransport<NetworkTransport>(new(networkHandler));

        _cdp.Listen(_cancellationTokenSource.Token);
        _cdp.Advertise(_cancellationTokenSource.Token);

        NearShareReceiver.Start(_cdp, this);
    }

    string GetFilePath(string name)
    {
        var downloadDir = Path.Combine(GetExternalMediaDirs()?.FirstOrDefault()?.AbsolutePath ?? "/sdcard/", "Download");
        if (!Directory.Exists(downloadDir))
            Directory.CreateDirectory(downloadDir);

        return Path.Combine(
            downloadDir,
            name
        );
    }

    FileStream CreateFile(string name)
    {
        var path = GetFilePath(name);
        Log($"Saving file to \"{path}\"");
        return File.Create(path);

        // ToDo: OutputStream cannot seek!
        //ContentValues contentValues = new();
        //contentValues.Put(MediaStore.IMediaColumns.RelativePath, Path.Combine(Android.OS.Environment.DirectoryDownloads!, name));
        //var uri = ContentResolver!.Insert(MediaStore.Files.GetContentUri("external")!, contentValues)!;
        //return ContentResolver!.OpenOutputStream(uri, "rwt")!;
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        if (!grantResults.All((x) => x != Permission.Granted))
            InitializeCDP();
        else
            Toast.MakeText(this, this.Localize(Resource.String.receive_missing_permissions), ToastLength.Long)!.Show();
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

    public void Log(string message)
    {
        RunOnUiThread(() =>
        {
            debugLogTextView.Text += "\n" + $"[{DateTime.Now:HH:mm:ss}]: {message}";
        });
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