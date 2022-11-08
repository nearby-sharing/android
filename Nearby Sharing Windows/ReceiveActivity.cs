using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using AndroidX.AppCompat.App;
using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;
using ShortDev.Microsoft.ConnectedDevices.Protocol.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Networking;
using System.Diagnostics;
using System.Net.NetworkInformation;
using AndroidUri = Android.Net.Uri;
using ManifestPermission = Android.Manifest.Permission;

namespace Nearby_Sharing_Windows;

[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", ConfigurationChanges = Constants.ConfigChangesFlags)]
public sealed class ReceiveActivity : AppCompatActivity, ICdpBluetoothHandler, ICdpPlatformHandler
{
    BluetoothAdapter? _btAdapter;
    BluetoothAdvertisement? _bluetoothAdvertisement;

    TextView debugLogTextView;
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // ToDo: Mac address settings
        // SetContentView(Resource.Layout.activity_mac_address);

        SetContentView(Resource.Layout.activity_receive);

        RequestPermissions(new[] {
            ManifestPermission.AccessFineLocation,
            ManifestPermission.AccessCoarseLocation,
            ManifestPermission.Bluetooth,
            ManifestPermission.BluetoothScan,
            ManifestPermission.BluetoothConnect,
            ManifestPermission.BluetoothAdvertise,
            ManifestPermission.AccessBackgroundLocation
        }, 0);

        //UdpAdvertisement advertisement = new();
        //advertisement.StartDiscovery();

        var service = (BluetoothManager)GetSystemService(BluetoothService)!;
        _btAdapter = service.Adapter!;

        string address = TryGetBtAddress(_btAdapter, out var exception) ?? "00:fa:21:3e:fb:19"; // "d4:38:9c:0b:ca:ae"; //

        FindViewById<TextView>(Resource.Id.deviceInfoTextView)!.Text = $"Visible as {_btAdapter.Name!}.\n" +
            $"Address: {address}";
        debugLogTextView = FindViewById<TextView>(Resource.Id.debugLogTextView)!;

        CdpAppRegistration.RegisterApp<NearShareHandshakeApp>(() => new() { PlatformHandler = this });

        _bluetoothAdvertisement = new(this);
        _bluetoothAdvertisement.OnDeviceConnected += BluetoothAdvertisement_OnDeviceConnected;
        _bluetoothAdvertisement.StartAdvertisement(new CdpDeviceAdvertiseOptions(
            DeviceType.Android,
            PhysicalAddress.Parse(address.Replace(":", "").ToUpper()),
            _btAdapter.Name!
        ));
    }

    public override void Finish()
    {
        _bluetoothAdvertisement?.StopAdvertisement();
        base.Finish();
    }

    [DebuggerHidden]
    public string? TryGetBtAddress(BluetoothAdapter adapter, out Exception? exception)
    {
        exception = null;

        try
        {
            var mServiceField = adapter.Class.GetDeclaredFields().FirstOrDefault((x) => x.Name.Contains("service", StringComparison.OrdinalIgnoreCase));
            if (mServiceField == null)
                throw new MissingFieldException("No service field found!");

            mServiceField.Accessible = true;
            var serviceProxy = mServiceField.Get(adapter)!;
            var method = serviceProxy.Class.GetDeclaredMethod("getAddress");
            if (method == null)
                throw new MissingMethodException("No method \"getAddress\"");

            method.Accessible = true;
            try
            {
                return (string?)method.Invoke(serviceProxy);
            }
            catch (Java.Lang.Reflect.InvocationTargetException ex)
            {
                if (ex.Cause == null)
                    throw;
                throw ex.Cause;
            }
        }
        catch (System.Exception ex)
        {
            exception = ex;
        }
        return null;
    }


    private void BluetoothAdvertisement_OnDeviceConnected(CdpRfcommSocket socket)
    {
        Log(0, $"Device {socket.RemoteDevice!.Name} ({socket.RemoteDevice!.Address}) connected via rfcomm");
        Task.Run(() =>
        {
            using (BigEndianBinaryWriter writer = new(socket.OutputStream!))
            using (BigEndianBinaryReader reader = new(socket.InputStream!))
            {
                bool expectMessage = true;
                CdpSession? session = null;
                while (expectMessage)
                {
                    try
                    {
                        var header = CommonHeader.Parse(reader);
                        session = CdpSession.GetOrCreate(socket.RemoteDevice ?? throw new InvalidDataException(), header);
                        session.PlatformHandler = this;
                        session.HandleMessage(socket, header, reader, writer, ref expectMessage);
                    }
                    catch (Exception ex)
                    {
                        Log(1, $"{ex.GetType().Name} in session {session?.LocalSessionId.ToString() ?? "null"} \n {ex.Message}");
                        throw;
                    }
                }
            }
        });
    }

    public Task ScanBLeAsync(CdpScanOptions<CdpBluetoothDevice> scanOptions, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<CdpRfcommSocket> ConnectRfcommAsync(CdpBluetoothDevice device, CdpRfcommOptions options, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

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

    public async Task ListenRfcommAsync(CdpRfcommOptions options, CancellationToken cancellationToken = default)
    {
        using (var listener = _btAdapter!.ListenUsingInsecureRfcommWithServiceRecord(
            options.ServiceName,
            Java.Util.UUID.FromString(options.ServiceId)
        )!)
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    var socket = listener.Accept();
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (socket != null)
                        options!.OnSocketConnected!(socket.ToCdp());
                }
            }, cancellationToken);
            await cancellationToken.AwaitCancellation();
            listener.Close();
        }
    }

    public void Log(int level, string message)
    {
        RunOnUiThread(() =>
        {
            debugLogTextView.Text += "\n" + $"[{DateTime.Now.ToString("HH:mm:ss")}]: {message}";
        });
    }

    public void LaunchUri(string uri)
    {
        StartActivity(new Intent(Intent.ActionView, AndroidUri.Parse(uri)!));
    }
}

static class Extensions
{
    public static CdpBluetoothDevice ToCdp(this BluetoothDevice @this, byte[]? beaconData = null)
        => new()
        {
            Address = @this.Address,
            Alias = @this.Alias,
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