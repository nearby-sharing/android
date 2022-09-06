#nullable enable

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;
using System;
using System.IO;
using System.Linq;

namespace Nearby_Sharing_Windows.Bluetooth
{
    [Service]
    public sealed class BluetoothLeService : Service
    {
        ServiceBinder _binder => new(this);
        public override IBinder OnBind(Intent? intent)
            => _binder;

        BluetoothAdapter? bluetoothAdapter;
        public bool TryInitialize()
        {
            bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            return bluetoothAdapter != null;
        }

        public void Connect(string address)
        {
            if (bluetoothAdapter == null)
                throw new InvalidOperationException("Not initialized!");

            var device = bluetoothAdapter.GetRemoteDevice(address);
            if (device == null)
                throw new ArgumentException(nameof(device));

            //System.Diagnostics.Debug.Print("Connecting via rfcomm...");
            //var socket = device.CreateInsecureRfcommSocketToServiceRecord(Java.Util.UUID.NameUUIDFromBytes(BluetoothAdvertisement.ServiceId.ToByteArray()));
            //socket.Connect();
            //ReceiveActivity.PrintStreamData(socket.InputStream);

            device.ConnectGatt(this, false, new GattCallback(), BluetoothTransports.Le);
        }

        class GattCallback : BluetoothGattCallback
        {
            public override void OnConnectionStateChange(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status, [GeneratedEnum] ProfileState newState)
            {
                if (newState == ProfileState.Connected)
                {

                }
                else
                    System.Diagnostics.Debug.Print($"Status: {status}; State: {newState}");
            }

            public override void OnServicesDiscovered(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status)
            {
                var services = gatt.Services.ToArray();
            }
        }
    }

    public sealed class ServiceBinder : Binder
    {
        public ServiceBinder(Service service)
           => Service = service;

        public Service Service { get; private set; }
    }
}