using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.ProgressIndicator;
using ShortDev.Android.UI;
using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;
using ShortDev.Microsoft.ConnectedDevices.Protocol.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Networking;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ManifestPermission = Android.Manifest.Permission;

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
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_receive);

        if (ReceiveSetupActivity.IsSetupRequired(this) || !ReceiveSetupActivity.TryGetBtAddress(this, out var btAddress) || btAddress == null)
        {
            StartActivity(new Android.Content.Intent(this, typeof(ReceiveSetupActivity)));

            Finish();
            return;
        }

        ActivityCompat.RequestPermissions(this, new[] {
            ManifestPermission.AccessFineLocation,
            ManifestPermission.AccessCoarseLocation,
            ManifestPermission.Bluetooth,
            ManifestPermission.BluetoothScan,
            ManifestPermission.BluetoothConnect,
            ManifestPermission.BluetoothAdvertise,
            ManifestPermission.AccessBackgroundLocation,
            ManifestPermission.ReadExternalStorage,
            ManifestPermission.WriteExternalStorage
        }, 0);

        notificationsRecyclerView = FindViewById<RecyclerView>(Resource.Id.notificationsRecyclerView)!;
        notificationsRecyclerView.SetLayoutManager(new LinearLayoutManager(this));

        adapterDescriptor = new(
            Resource.Layout.item_transfer_notification,
            (view, transfer) =>
            {
                var acceptButton = view.FindViewById<Button>(Resource.Id.acceptButton)!;
                var fileNameTextView = view.FindViewById<TextView>(Resource.Id.fileNameTextView)!;
                var detailsTextView = view.FindViewById<TextView>(Resource.Id.detailsTextView)!;

                if (transfer is FileTransferToken fileTransfer)
                {
                    fileNameTextView.Text = fileTransfer.FileName;
                    detailsTextView.Text = $"{fileTransfer.DeviceName} • {FileTransferToken.FormatFileSize(fileTransfer.FileSize)}";

                    var loadingProgressIndicator = view.FindViewById<CircularProgressIndicator>(Resource.Id.loadingProgressIndicator)!;
                    acceptButton.Click += (s, e) =>
                    {
                        fileTransfer.Accept(CreateFile(fileTransfer.FileName));
                        view.FindViewById(Resource.Id.actionsContainer)!.Visibility = ViewStates.Gone;

                        loadingProgressIndicator.Visibility = ViewStates.Visible;
                        loadingProgressIndicator.Progress = 0;
                        fileTransfer.Progress += (s) => RunOnUiThread(() =>
                        {
                            int progress = Math.Min((int)(fileTransfer.ReceivedBytes * 100 / fileTransfer.FileSize), 100);
                            if (OperatingSystem.IsAndroidVersionAtLeast(24))
                                loadingProgressIndicator.SetProgress(progress, true);
                            else
                                loadingProgressIndicator.Progress = progress;
                        });
                    };
                }
                else if (transfer is UriTranferToken uriTranfer)
                {
                    fileNameTextView.Text = uriTranfer.Uri;
                    detailsTextView.Text = uriTranfer.DeviceName;

                    acceptButton.Click += (s, e) => UIHelper.DisplayWebSite(this, uriTranfer.Uri);
                }

                view.FindViewById<Button>(Resource.Id.cancelButton)!.Click += (s, e) =>
                {
                    _notifications.Remove(transfer);
                    UpdateUI();

                    if (transfer is FileTransferToken fileTransfer)
                        fileTransfer.Cancel();
                };
            }
        );

        var service = (BluetoothManager)GetSystemService(BluetoothService)!;
        _btAdapter = service.Adapter!;

        FindViewById<TextView>(Resource.Id.deviceInfoTextView)!.Text = $"Visible as {_btAdapter.Name!}.\n" +
            $"Address: {btAddress}";
        debugLogTextView = FindViewById<TextView>(Resource.Id.debugLogTextView)!;

        CdpAppRegistration.TryUnregisterApp<NearShareHandshakeApp>();
        CdpAppRegistration.TryRegisterApp<NearShareHandshakeApp>(() => new() { PlatformHandler = this });

        _bluetoothAdvertisement = new(this);
        _bluetoothAdvertisement.OnDeviceConnected += BluetoothAdvertisement_OnDeviceConnected;
        _bluetoothAdvertisement.StartAdvertisement(new CdpDeviceAdvertiseOptions(
            DeviceType.Android,
            btAddress, // "00:fa:21:3e:fb:19"
            _btAdapter.Name!
        ));
    }

    Stream CreateFile(string name)
    {
        var downloadDir = Path.Combine(GetExternalMediaDirs()?.FirstOrDefault()?.AbsolutePath ?? "/sdcard/", "Download");
        if (!Directory.Exists(downloadDir))
            Directory.CreateDirectory(downloadDir);

        var path = Path.Combine(
            downloadDir,
            name
        );
        Log(0, $"Saving file to \"{path}\"");
        return File.Create(path);

        // OutputStream cannot seek!
        //ContentValues contentValues = new();
        //contentValues.Put(MediaStore.IMediaColumns.RelativePath, Path.Combine(Android.OS.Environment.DirectoryDownloads!, name));
        //var uri = ContentResolver!.Insert(MediaStore.Files.GetContentUri("external")!, contentValues)!;
        //return ContentResolver!.OpenOutputStream(uri, "rwt")!;
    }

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

    public Task<CdpRfcommSocket> ConnectRfcommAsync(CdpBluetoothDevice device, CdpRfcommOptions options, CancellationToken cancellationToken = default)
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
        using (var securelistener = _btAdapter!.ListenUsingRfcommWithServiceRecord(
            options.ServiceName,
            Java.Util.UUID.FromString(options.ServiceId)
        )!)
        using (var insecureListener = _btAdapter!.ListenUsingInsecureRfcommWithServiceRecord(
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

    private void BluetoothAdvertisement_OnDeviceConnected(CdpRfcommSocket socket)
    {
        Log(0, $"Device {socket.RemoteDevice!.Name} ({socket.RemoteDevice!.Address}) connected via rfcomm");
        Task.Run(() =>
        {
            using (BigEndianBinaryWriter writer = new(socket.OutputStream!))
            using (BigEndianBinaryReader reader = new(socket.InputStream!))
            {
                CancellationTokenSource cancellationTokenSource = new();
                do
                {
                    CdpSession? session = null;
                    try
                    {
                        var header = CommonHeader.Parse(reader);
                        session = CdpSession.GetOrCreate(socket.RemoteDevice ?? throw new InvalidDataException(), header);
                        session.PlatformHandler = this;
                        session.HandleMessage(header, reader, writer);
                    }
                    catch (Exception ex)
                    {
                        Log(1, $"{ex.GetType().Name} in session {session?.LocalSessionId.ToString() ?? "null"} \n {ex.Message}");
                        throw;
                    }
                } while (!cancellationTokenSource.IsCancellationRequested);
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

    public static CdpRfcommSocket ToCdp(this BluetoothSocket @this)
        => new()
        {
            InputStream = @this.InputStream,
            OutputStream = @this.OutputStream,
            RemoteDevice = @this.RemoteDevice!.ToCdp(),
            Close = @this.Close
        };
}