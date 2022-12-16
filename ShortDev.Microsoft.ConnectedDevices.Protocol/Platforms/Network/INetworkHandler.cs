namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms.Network;

public interface INetworkHandler : ICdpPlatformHandler
{
    string GetLocalIP();
}
