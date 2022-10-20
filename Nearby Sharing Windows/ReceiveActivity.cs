#nullable enable

using Android.Bluetooth;
using Android.Bluetooth.LE;
using AndroidX.AppCompat.App;
using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Networking;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using ConnectionRequest = ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.ConnectionRequest;
using ManifestPermission = Android.Manifest.Permission;

namespace Nearby_Sharing_Windows
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ReceiveActivity : AppCompatActivity, ICdpBluetoothHandler
    {
        BluetoothAdapter? _btAdapter;
        BluetoothAdvertisement? _bluetoothAdvertisement;
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

        CdpEncryptionInfo localEncryption = CdpEncryptionInfo.Create();
        CdpEncryptionInfo? remoteEncryption;
        private void BluetoothAdvertisement_OnDeviceConnected(CdpRfcommSocket socket)
        {
            Task.Run(() =>
            {
                using (MemoryStream testStream = new())
                using (BigEndianBinaryWriter writer = new(socket.OutputStream!))
                using (BigEndianBinaryReader reader = new(socket.InputStream!))
                {
                    while (true)
                    {
                        if (!CommonHeader.TryParse(reader, out var header, out _) || header == null)
                            return;

                        foreach (var entry in header.AdditionalHeaders)
                        {
                            Debug.Print(entry.Type + " " + BinaryConvert.ToString(entry.Value));
                        }

                        if (header.Flags.HasFlag(MessageFlags.SessionEncrypted))
                        {
                            using (BigEndianBinaryWriter writer2 = new(testStream))
                            {
                                header.Write(writer2);
                                int length = header.MessageLength - (int)((ICdpSerializable<CommonHeader>)header).CalcSize();
                                writer2.Write(reader.ReadBytes(length));
                                Debug.Print(BinaryConvert.ToString(testStream.ToArray()));
                            }

                            continue; // ToDo: remove
                        }

                        if (header.Type == MessageType.Connect)
                        {
                            ConnectionHeader connectionHeader = ConnectionHeader.Parse(reader);
                            switch (connectionHeader.ConnectMessageType)
                            {
                                case ConnectionType.ConnectRequest:
                                    {
                                        var connectionRequest = ConnectionRequest.Parse(reader);
                                        remoteEncryption = CdpEncryptionInfo.FromRemote(connectionRequest.PublicKeyX, connectionRequest.PublicKeyY, connectionRequest.Nonce);

                                        header.AdditionalHeaders = new CommonHeader.AdditionalMessageHeader[]
                                        {
                                            new(
                                                (NextHeaderType)129,
                                                new byte[]{ 0x70,0x00,0x00,0x03 }
                                            ),
                                            new(
                                                (NextHeaderType)130,
                                                new byte[]{ 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x1F }
                                            ),
                                            new(
                                                (NextHeaderType)131,
                                                new byte[]{ 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x1F }
                                            )
                                        };

                                        header.SessionID = 0x0000000e80000000 + header.SessionID;

                                        header.Write(writer);

                                        new ConnectionHeader()
                                        {
                                            ConnectionMode = ConnectionMode.Proximal,
                                            ConnectMessageType = ConnectionType.ConnectResponse
                                        }.Write(writer);

                                        var publicKey = localEncryption.PublicKey;
                                        new ConnectionResponse()
                                        {
                                            Result = ConnectionResult.Pending,
                                            HMACSize = connectionRequest.HMACSize,
                                            MessageFragmentSize = connectionRequest.MessageFragmentSize,
                                            Nonce = localEncryption.Nonce,
                                            PublicKeyX = publicKey.X!,
                                            PublicKeyY = publicKey.Y!
                                        }.Write(writer);

                                        writer.Flush();
                                        Debug.Print(BinaryConvert.ToString(localEncryption.GenerateSharedSecret(remoteEncryption)));
                                        Debug.Print(BinaryConvert.ToString(testStream.ToArray()));

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

            await AwaitCancellation(cancellationToken);

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
                await AwaitCancellation(cancellationToken);
                listener.Close();
            }
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