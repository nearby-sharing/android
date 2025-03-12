using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices;

partial class ConnectedDevicesPlatform
{
    public IRemoteSystemAdvertiser CreateAdvertiser()
        => new Advertiser(this);

    readonly ReferenceCounter _advertisementCounter = new();
    private sealed class Advertiser(ConnectedDevicesPlatform cdp) : IRemoteSystemAdvertiser
    {
        readonly ConnectedDevicesPlatform _cdp = cdp;
        readonly ILogger<Advertiser> _logger = cdp.CreateLogger<Advertiser>();

        int _started = 0;
        public async ValueTask Start(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _started, 1, 0) == 1)
                return;

            if (!_cdp._advertisementCounter.Add())
                return;

            _logger.AdvertisingStarted();
            try
            {
                await Task.WhenAll(_cdp._transportMap.Values
                    .OfType<ICdpDiscoverableTransport>()
                    .Select(x => x.StartAdvertisement(_cdp.DeviceInfo, cancellationToken).AsTask())
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.AdvertisingError(ex);
            }
        }

        public async ValueTask Stop(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _started, 0, 1) == 0)
                return;

            if (!_cdp._advertisementCounter.Release())
                return;

            try
            {
                await Task.WhenAll(_cdp._transportMap.Values
                    .OfType<ICdpDiscoverableTransport>()
                    .Select(x => x.StopAdvertisement(cancellationToken).AsTask())
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.AdvertisingError(ex);
            }
            finally
            {
                _logger.AdvertisingStopped();
            }
        }

        public ValueTask DisposeAsync()
            => Stop(CancellationToken.None);
    }
}

public interface IRemoteSystemAdvertiser : IAsyncDisposable
{
    ValueTask Start(CancellationToken cancellationToken = default);
    ValueTask Stop(CancellationToken cancellationToken = default);
}
