using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Microsoft.Extensions.Logging;
using NearShare.Droid.Settings;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;
using ShortDev.Microsoft.ConnectedDevices.Transports.Network;
using System.Net.NetworkInformation;

namespace NearShare.Droid.Utils;

internal static class CdpUtils
{
    public static ConnectedDevicesPlatform Create(Context context, ILoggerFactory loggerFactory)
    {
        var btService = (BluetoothManager)context.GetSystemService(Context.BluetoothService)!;
        var btAdapter = btService.Adapter ?? throw new NullReferenceException("Could not get bt adapter");

        if (!ReceiveSetupActivity.TryGetBtAddress(context, out var btAddress))
            btAddress = PhysicalAddress.None;

        LocalDeviceInfo deviceInfo = new()
        {
            Type = DeviceType.Android,
            Name = SettingsFragment.GetDeviceName(context, btAdapter),
            OemModelName = Build.Model ?? string.Empty,
            OemManufacturerName = Build.Manufacturer ?? string.Empty,
            DeviceCertificate = ConnectedDevicesPlatform.CreateDeviceCertificate(CdpEncryptionParams.Default)
        };

        ConnectedDevicesPlatform cdp = new(deviceInfo, loggerFactory);

        AndroidBluetoothHandler bluetoothHandler = new(btAdapter, btAddress);
        cdp.AddTransport<BluetoothTransport>(new(bluetoothHandler));

        AndroidNetworkHandler networkHandler = new(context);
        cdp.AddTransport<NetworkTransport>(new(networkHandler));

        return cdp;
    }
}
