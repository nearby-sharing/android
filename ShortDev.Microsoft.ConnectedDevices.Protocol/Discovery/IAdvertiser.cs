namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery
{
    public interface IAdvertiser
    {
        void StartAdvertisement();
        void StopAdvertisement();
    }
}