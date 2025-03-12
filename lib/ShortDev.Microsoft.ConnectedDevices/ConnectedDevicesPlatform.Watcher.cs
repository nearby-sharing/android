using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices;

partial class ConnectedDevicesPlatform
{
    public IRemoteSystemWatcher CreateWatcher()
        => new Watcher(this);

    readonly ReferenceCounter _watcherCounter = new();
    private sealed class Watcher(ConnectedDevicesPlatform cdp) : IRemoteSystemWatcher
    {
        readonly ConnectedDevicesPlatform _cdp = cdp;
        readonly ILogger<Watcher> _logger = cdp.CreateLogger<Watcher>();

        public event EventHandler<CdpDevice>? RemoteSystemAdded;
        public event EventHandler<CdpDevice>? RemoteSystemRemoved;
        public event EventHandler<CdpDevice>? RemoteSystemUpdated;

        readonly HashSet<CdpDevice> _devices = [];
        private void OnDeviceDiscovered(ICdpTransport sender, CdpDevice device)
        {
            if (_devices.Add(device))
                RemoteSystemAdded?.Invoke(this, device);
        }

        int _started = 0;
        public async ValueTask Start(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _started, 1, 0) == 1)
                return;

            if (!_cdp._watcherCounter.Add())
                return;

            _logger.DiscoveryStarted(_cdp._transportMap.Values.Select(x => x.TransportType));
            try
            {
                await Task.WhenAll(_cdp._transportMap.Values
                    .OfType<ICdpDiscoverableTransport>()
                    .Select(async transport =>
                    {
                        transport.DeviceDiscovered += OnDeviceDiscovered;
                        await transport.StartDiscovery(cancellationToken).ConfigureAwait(false);
                    })
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.DiscoveryError(ex);
            }
        }

        public async ValueTask Stop(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _started, 0, 1) == 0)
                return;

            if (!_cdp._watcherCounter.Release())
                return;

            try
            {
                await Task.WhenAll(_cdp._transportMap.Values
                    .OfType<ICdpDiscoverableTransport>()
                    .Select(async transport =>
                    {
                        transport.DeviceDiscovered -= OnDeviceDiscovered;
                        await transport.StopDiscovery(cancellationToken).ConfigureAwait(false);
                    })
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.DiscoveryError(ex);
            }
            finally
            {
                _logger.DiscoveryStopped();
            }
        }

        public ValueTask DisposeAsync()
            => Stop(CancellationToken.None);
    }
}

public interface IRemoteSystemWatcher : IAsyncDisposable
{
    ValueTask Start(CancellationToken cancellationToken = default);
    ValueTask Stop(CancellationToken cancellationToken = default);

    event EventHandler<CdpDevice>? RemoteSystemAdded;
    event EventHandler<CdpDevice>? RemoteSystemRemoved;
    event EventHandler<CdpDevice>? RemoteSystemUpdated;
}
