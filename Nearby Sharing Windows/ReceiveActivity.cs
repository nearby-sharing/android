using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.ProgressIndicator;
using ShortDev.Android.UI;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Nearby_Sharing_Windows;

[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class ReceiveActivity : AppCompatActivity, ICdpBluetoothHandler, INearSharePlatformHandler
{
    BluetoothAdapter? _btAdapter;
    BluetoothAdvertisement? _bluetoothAdvertisement;

    [AllowNull] TextView debugLogTextView;

    [AllowNull] AdapterDescriptor<TranferToken> adapterDescriptor;
    [AllowNull] RecyclerView notificationsRecyclerView;
    readonly List<TranferToken> _notifications = new();

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
        UIHelper.SetupToolBar(this, "Receive data from Windows 10 / 11");

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
                    fileNameTextView.Text = fileTransfer.FileName;
                    detailsTextView.Text = $"{fileTransfer.DeviceName} • {FileTransferToken.FormatFileSize(fileTransfer.FileSize)}";

                    var loadingProgressIndicator = view.FindViewById<CircularProgressIndicator>(Resource.Id.loadingProgressIndicator)!;
                    Action onCompleted = () =>
                    {
                        acceptButton.Visibility = ViewStates.Gone;
                        loadingProgressIndicator.Visibility = ViewStates.Gone;
                        openButton.Visibility = ViewStates.Visible;
                        openButton.SetOnClickListener(new DelegateClickListener((s, e) => UIHelper.OpenFile(this, GetFilePath(fileTransfer.FileName))));
                    };
                    if (fileTransfer.IsTransferComplete)
                        onCompleted();
                    else
                    {
                        Action onAccept = () =>
                        {
                            if (!fileTransfer.IsAccepted)
                                fileTransfer.Accept(CreateFile(fileTransfer.FileName));

                            acceptButton.Visibility = ViewStates.Gone;
                            loadingProgressIndicator.Visibility = ViewStates.Visible;

                            loadingProgressIndicator.Progress = 0;
                            Action<bool> onProgress = (animate) =>
                            {
                                loadingProgressIndicator.Indeterminate = false;

                                int progress = Math.Min((int)(fileTransfer.ReceivedBytes * 100 / fileTransfer.FileSize), 100);
                                if (OperatingSystem.IsAndroidVersionAtLeast(24))
                                    loadingProgressIndicator.SetProgress(progress, animate);
                                else
                                    loadingProgressIndicator.Progress = progress;

                                if (fileTransfer.IsTransferComplete)
                                    onCompleted();
                            };
                            fileTransfer.SetProgressListener((s) => RunOnUiThread(() => onProgress(/*animate*/true)));
                            loadingProgressIndicator.Indeterminate = true;
                        };
                        if (fileTransfer.IsAccepted)
                            onAccept();
                        else
                            acceptButton.SetOnClickListener(new DelegateClickListener((s, e) => onAccept()));
                    }
                }
                else if (transfer is UriTranferToken uriTranfer)
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

    void InitializeCDP()
    {
        if (btAddress == null)
            throw new NullReferenceException(nameof(btAddress));

        var service = (BluetoothManager)GetSystemService(BluetoothService)!;
        _btAdapter = service.Adapter!;

        FindViewById<TextView>(Resource.Id.deviceInfoTextView)!.Text = $"Visible as {_btAdapter.Name!}.\n" +
            $"Address: {btAddress.ToStringFormatted()}";
        debugLogTextView = FindViewById<TextView>(Resource.Id.debugLogTextView)!;

        CdpAppRegistration.TryUnregisterApp<NearShareHandshakeApp>();
        CdpAppRegistration.TryRegisterApp<NearShareHandshakeApp>(() => new() { PlatformHandler = this });

        _bluetoothAdvertisement = new(this);
        _bluetoothAdvertisement.OnDeviceConnected += OnDeviceConnected;
        _bluetoothAdvertisement.StartAdvertisement(new CdpDeviceAdvertiseOptions(
            DeviceType.Android,
            btAddress, // "00:fa:21:3e:fb:19"
            _btAdapter.Name!
        ));

        NetworkAdvertisement networkAdvertisement = new();
        networkAdvertisement.OnDeviceConnected += OnDeviceConnected;
        networkAdvertisement.StartAdvertisement(null);
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
        Log(0, $"Saving file to \"{path}\"");
        return File.Create(path);

        // OutputStream cannot seek!
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
            Toast.MakeText(this, "Can't receive without permissions!", ToastLength.Long)!.Show();
    }

    public override bool OnCreateOptionsMenu(IMenu? menu)
        => UIHelper.OnCreateOptionsMenu(this, menu);

    public override bool OnOptionsItemSelected(IMenuItem item)
        => UIHelper.OnOptionsItemSelected(this, item);

    public override void Finish()
    {
        _bluetoothAdvertisement?.StopAdvertisement();
        base.Finish();
    }

    #region Communication
    #region Not implemented
    public Task ScanBLeAsync(CdpScanOptions<CdpBluetoothDevice> scanOptions, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<CdpSocket> ConnectRfcommAsync(CdpBluetoothDevice device, CdpRfcommOptions options, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    #endregion

    #region Advertisement
    public async Task AdvertiseBLeBeaconAsync(CdpAdvertiseOptions options, CancellationToken cancellationToken = default)
    {
        var settings = new AdvertiseSettings.Builder()
            .SetAdvertiseMode(AdvertiseMode.LowLatency)!
            .SetTxPowerLevel(AdvertiseTx.PowerHigh)!
            .SetConnectable(false)!
            .Build();

        var data = new AdvertiseData.Builder()
            .AddManufacturerData(options.ManufacturerId, options.BeaconData!)!
            .Build();

        BLeAdvertiseCallback callback = new();
        _btAdapter!.BluetoothLeAdvertiser!.StartAdvertising(settings, data, callback);

        await cancellationToken.AwaitCancellation();

        _btAdapter.BluetoothLeAdvertiser.StopAdvertising(callback);
    }

    class BLeAdvertiseCallback : AdvertiseCallback { }
    #endregion

    #region Rfcomm
    public async Task ListenRfcommAsync(CdpRfcommOptions options, CancellationToken cancellationToken = default)
    {
        if (_btAdapter == null)
            throw new InvalidOperationException($"{nameof(_btAdapter)} is null");

        using (var insecureListener = _btAdapter.ListenUsingInsecureRfcommWithServiceRecord(
            options.ServiceName,
            Java.Util.UUID.FromString(options.ServiceId)
        )!)
        using (var securelistener = _btAdapter.ListenUsingRfcommWithServiceRecord(
            options.ServiceName,
            Java.Util.UUID.FromString(options.ServiceId)
        )!)
        {
            Func<BluetoothServerSocket, Task> processor = async (listener) =>
            {
                while (true)
                {
                    var socket = await listener.AcceptAsync();
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (socket != null)
                        options!.OnSocketConnected!(socket.ToCdp());
                }
            };
            await Task.WhenAny(new[] {
                Task.Run(() => processor(securelistener), cancellationToken),
                Task.Run(() => processor(insecureListener), cancellationToken)
            });
        }
    }

    private void OnDeviceConnected(CdpSocket socket)
    {
        Log(0, $"Device {socket.RemoteDevice!.Name} ({socket.RemoteDevice!.Address}) connected via rfcomm");
        Task.Run(() =>
        {
            var reader = socket.Reader;
            using (socket)
            {
                do
                {
                    CdpSession? session = null;
                    try
                    {
                        var header = CommonHeader.Parse(reader);
                        session = CdpSession.GetOrCreate(socket.RemoteDevice ?? throw new InvalidDataException(), header);
                        session.PlatformHandler = this;
                        session.HandleMessage(socket, header, reader);
                    }
                    catch (Exception ex)
                    {
                        Log(1, $"{ex.GetType().Name} in session {session?.LocalSessionId.ToString() ?? "null"} \n {ex.Message}");
                        throw;
                    }
                } while (!socket.IsClosed);
            }
        });
    }
    #endregion
    #endregion

    public void Log(int level, string message)
    {
        RunOnUiThread(() =>
        {
            debugLogTextView.Text += "\n" + $"[{DateTime.Now.ToString("HH:mm:ss")}]: {message}";
        });
    }

    public string GetLocalIP()
    {
        WifiManager wifiManager = (WifiManager)GetSystemService(WifiService)!;
        WifiInfo wifiInfo = wifiManager.ConnectionInfo!;
        int ip = wifiInfo.IpAddress;
        return new IPAddress(ip).ToString();
    }

    void UpdateUI()
    {
        RunOnUiThread(() =>
        {
            notificationsRecyclerView.SetAdapter(adapterDescriptor.CreateRecyclerViewAdapter(_notifications));
        });
    }

    public void OnReceivedUri(UriTranferToken transfer)
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
    public static CdpBluetoothDevice ToCdp(this BluetoothDevice @this, byte[]? beaconData = null)
        => new()
        {
            Address = @this.Address,
            Alias = OperatingSystem.IsAndroidVersionAtLeast(30) ? @this.Alias : null,
            Name = @this.Name,
            BeaconData = beaconData
        };

    public static CdpSocket ToCdp(this BluetoothSocket @this)
        => new()
        {
            InputStream = @this.InputStream,
            OutputStream = @this.OutputStream,
            RemoteDevice = @this.RemoteDevice!.ToCdp(),
            Close = @this.Close
        };
}