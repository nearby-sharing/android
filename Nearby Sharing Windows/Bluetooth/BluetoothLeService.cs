#nullable enable

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;

namespace Nearby_Sharing_Windows.Bluetooth
{
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
    }

    public sealed class ServiceBinder : Binder
    {
        public ServiceBinder(Service service)
           => Service = service;

        public Service Service { get; private set; }
    }
}