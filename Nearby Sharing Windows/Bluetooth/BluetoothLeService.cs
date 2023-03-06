using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;

namespace Nearby_Sharing_Windows.Bluetooth;

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

    BluetoothGatt? connection;
    public void Connect(string address)
    {
        if (connection != null)
            return;

        if (bluetoothAdapter == null)
            throw new InvalidOperationException("Not initialized!");

        var device = bluetoothAdapter.GetRemoteDevice(address);
        if (device == null)
            throw new ArgumentException(nameof(device));

        connection = device.ConnectGatt(this, false, new GattCallback(), BluetoothTransports.Le);
        // var result = connection.Connect();
    }

    class GattCallback : BluetoothGattCallback
    {
        public override void OnConnectionStateChange(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status, [GeneratedEnum] ProfileState newState)
        {
            if (newState == ProfileState.Connected)
            {
                gatt?.DiscoverServices();
            }
            else
                System.Diagnostics.Debug.Print($"Status: {status}; State: {newState}");
        }

        public override void OnServicesDiscovered(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status)
        {
            var services = gatt.Services.ToArray();
        }

        public override void OnServiceChanged(BluetoothGatt gatt)
        {
            base.OnServiceChanged(gatt);
        }

        public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
        {
            base.OnCharacteristicChanged(gatt, characteristic);
        }
    }
}

public sealed class ServiceBinder : Binder
{
    public ServiceBinder(Service service)
       => Service = service;

    public Service Service { get; private set; }
}