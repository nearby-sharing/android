using System.Net.NetworkInformation;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;

public interface IBluetoothHandler
{
    Task ScanBLeAsync(ScanOptions scanOptions, CancellationToken cancellationToken = default);
    Task<CdpSocket> ConnectRfcommAsync(EndpointInfo device, RfcommOptions options, CancellationToken cancellationToken = default);

    Task AdvertiseBLeBeaconAsync(AdvertiseOptions options, CancellationToken cancellationToken = default);
    Task ListenRfcommAsync(RfcommOptions options, CancellationToken cancellationToken = default);

    PhysicalAddress MacAddress { get; }
    bool IsEnabled => true;
}
