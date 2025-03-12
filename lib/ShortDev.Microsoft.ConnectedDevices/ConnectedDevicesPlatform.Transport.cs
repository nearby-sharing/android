using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Collections.Concurrent;

namespace ShortDev.Microsoft.ConnectedDevices;

partial class ConnectedDevicesPlatform
{
    readonly ConcurrentDictionary<CdpTransportType, ICdpTransport> _transportMap = new();
    public void AddTransport<T>(T transport) where T : ICdpTransport
    {
        _transportMap.AddOrUpdate(
            transport.TransportType,
            static (key, newTansport) => newTansport,
            static (key, oldTransport, newTansport) =>
            {
                oldTransport.Dispose();
                return newTansport;
            },
            transport
        );
    }

    [Obsolete("Use overload instead")]
    public T? TryGetTransport<T>() where T : ICdpTransport
        => (T?)_transportMap.Values.SingleOrDefault(x => x is T);

    public ICdpTransport? TryGetTransport(CdpTransportType transportType)
        => _transportMap.GetValueOrDefault(transportType);
}
