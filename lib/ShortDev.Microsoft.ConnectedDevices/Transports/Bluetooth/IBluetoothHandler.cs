using System.Net.NetworkInformation;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;

public interface IBluetoothHandler
{
    ValueTask StartScanBle(ScanOptions scanOptions, CancellationToken cancellationToken);
    ValueTask StopScanBle(CancellationToken cancellationToken);

    Task<CdpSocket> ConnectRfcommAsync(EndpointInfo device, RfcommOptions options, CancellationToken cancellationToken);

    ValueTask StartAdvertiseBle(AdvertiseOptions options, CancellationToken cancellationToken);
    ValueTask StopAdvertiseBle(CancellationToken cancellationToken);

    ValueTask StartListenRfcomm(RfcommOptions options, CancellationToken cancellationToken);
    ValueTask StopListenRfcomm(CancellationToken cancellationToken);

    PhysicalAddress MacAddress { get; }
    bool IsEnabled { get; }
}
