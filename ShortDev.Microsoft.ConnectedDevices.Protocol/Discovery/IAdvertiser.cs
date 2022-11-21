namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;

public interface IAdvertiser
{
    void StartAdvertisement(CdpDeviceAdvertiseOptions options);
    void StopAdvertisement();
}