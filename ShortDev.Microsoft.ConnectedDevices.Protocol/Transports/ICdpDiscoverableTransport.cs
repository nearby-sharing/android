using System.Threading;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Transports;

public interface ICdpDiscoverableTransport : ICdpTransport
{
    void Advertise(CdpAdvertiseOptions options, CancellationToken cancellationToken);
}
