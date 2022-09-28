#nullable enable

using Android.Bluetooth;
using Android.Bluetooth.LE;
using AndroidX.AppCompat.App;
using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Networking;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using ManifestPermission = Android.Manifest.Permission;
using Stream = System.IO.Stream;

namespace Nearby_Sharing_Windows
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ReceiveActivity : AppCompatActivity, ICdpBluetoothHandler
    {
        BluetoothAdapter? _btAdapter;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_mac_address);

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

            BluetoothAdvertisement bluetoothAdvertisement = new(this);
            bluetoothAdvertisement.OnDeviceConnected += BluetoothAdvertisement_OnDeviceConnected;
            bluetoothAdvertisement.StartAdvertisement(new CdpDeviceAdvertiseOptions(
                DeviceType.Android,
                PhysicalAddress.Parse(address.Replace(":", "").ToUpper()),
                _btAdapter.Name!
            ));
        }

        public string? TryGetBtAddress(BluetoothAdapter adapter, out System.Exception? exception)
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

        ConnectionRequest connectionRequest;
        ECDsa ownKey;
        ECDsa remoteKey;
        private void BluetoothAdvertisement_OnDeviceConnected(CdpRfcommSocket socket)
        {
            Task.Run(() =>
            {
                using (BigEndianBinaryWriter writer = new(socket.OutputStream!))
                using (BigEndianBinaryReader reader = new(socket.InputStream!))
                {
                    while (true)
                    {
                        if (!CommonHeaders.TryParse(reader, out var headers, out _) || headers == null)
                            return;

                        if (headers.Type == MessageType.Connect)
                        {
                            ConnectionHeader connectionHeader = ConnectionHeader.Parse(reader);
                            switch (connectionHeader.ConnectMessageType)
                            {
                                case ConnectionType.ConnectRequest:
                                    {
                                        connectionRequest = ConnectionRequest.Parse(reader);

                                        remoteKey = ECDsa.Create(new ECParameters()
                                        {
                                            Curve = ECCurve.NamedCurves.nistP256,
                                            Q = new ECPoint()
                                            {
                                                X = connectionRequest.PublicKeyX,
                                                Y = connectionRequest.PublicKeyY
                                            }
                                        });

                                        headers.Write(writer);

                                        //new CommonHeaders()
                                        //{
                                        //    Type = MessageType.Connect,
                                        //    MessageLength = headers.MessageLength
                                        //}.Write(writer);

                                        new ConnectionHeader()
                                        {
                                            ConnectionMode = ConnectionMode.Proximal,
                                            ConnectMessageType = ConnectionType.ConnectResponse
                                        }.Write(writer);

                                        ownKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                                        var publicKey = ownKey.ExportExplicitParameters(false).Q;

                                        new ConnectionResponse()
                                        {
                                            Result = ConnectionResult.Pending,
                                            HMACSize = connectionRequest.HMACSize,
                                            MessageFragmentSize = connectionRequest.MessageFragmentSize,
                                            Nonce = connectionRequest.Nonce,
                                            PublicKeyX = publicKey.X!,
                                            PublicKeyY = publicKey.Y!
                                        }.Write(writer);

                                        writer.Flush();

                                        break;
                                    }
                                case ConnectionType.DeviceAuthRequest:
                                    {
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }
                        }
                    }
                }
            });
        }

        byte[] GenerateNonce()
            => new byte[64];

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
            _btAdapter.BluetoothLeAdvertiser!.StartAdvertising(settings, data, callback);

            await AwaitCancellation(cancellationToken);

            _btAdapter.BluetoothLeAdvertiser.StopAdvertising(callback);
        }
        class BLeAdvertiseCallback : AdvertiseCallback { }

        public async Task ListenRfcommAsync(CdpRfcommOptions options, CancellationToken cancellationToken = default)
        {
            var listener = _btAdapter.ListenUsingRfcommWithServiceRecord(
                options.ServiceName,
                Java.Util.UUID.FromString(options.ServiceId)
            )!;
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
        }

        Task AwaitCancellation(CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> promise = new();
            cancellationToken.Register(() => promise.SetResult(true));
            return promise.Task;
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
                Close = () => @this.Close()
            };
    }
}