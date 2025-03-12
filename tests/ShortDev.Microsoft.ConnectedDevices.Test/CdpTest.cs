using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Test.E2E;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Test;

public class CdpTest(ITestOutputHelper outputHelper)
{
    ConnectedDevicesPlatform CreatePlatform(string name)
    {
        LocalDeviceInfo DeviceInfo = new()
        {
            Name = name,
            OemManufacturerName = name,
            OemModelName = name,
            Type = DeviceType.Linux,
            DeviceCertificate = ConnectedDevicesPlatform.CreateDeviceCertificate(CdpEncryptionParams.Default)
        };

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(new TestLoggerProvider(name, outputHelper));
        });

        return new(DeviceInfo, loggerFactory);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCallListenOnce()
    {
        var platform = CreatePlatform("TestDevice");

        SpyTransport transport = new();
        platform.AddTransport(transport);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await platform.InitializeAsync(TestContext.Current.CancellationToken);
        await platform.InitializeAsync(TestContext.Current.CancellationToken);
        await platform.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await platform.DisposeAsync();

        Assert.Equal(1, transport.StartListenCalled);
        Assert.Equal(1, transport.StopListenCalled);
        Assert.Equal(1, transport.DisposeCalled);
    }

    [Fact]
    public void AddTransport_ShouldDisposeOldTransport_WhenTransportTypeAlreadyExists()
    {
        var platform = CreatePlatform("TestDevice");

        SpyTransport transport1 = new();
        SpyTransport transport2 = new();

        Assert.Equal(0, transport1.DisposeCalled);
        Assert.Equal(0, transport2.DisposeCalled);

        platform.AddTransport(transport1);
        platform.AddTransport(transport2);

        Assert.Equal(1, transport1.DisposeCalled);
        Assert.Equal(0, transport2.DisposeCalled);

        Assert.Equal(transport2, platform.TryGetTransport(CdpTransportType.Unknown));
    }

    [Fact]
    public async Task SingleWatcher_ShouldStartOnce()
    {
        var platform = CreatePlatform("TestDevice");

        SpyDiscoveryTransport transport = new();
        platform.AddTransport(transport);

        var watcher = platform.CreateWatcher();

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await watcher.Start(TestContext.Current.CancellationToken);
        await watcher.Start(TestContext.Current.CancellationToken);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(1, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await watcher.Stop(TestContext.Current.CancellationToken);
        await watcher.Stop(TestContext.Current.CancellationToken);
        await watcher.DisposeAsync();

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(1, transport.StartDiscoveryCalled);
        Assert.Equal(1, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);
    }

    [Fact]
    public async Task MultipleWatchers_ShouldStartOnce()
    {
        var platform = CreatePlatform("TestDevice");

        SpyDiscoveryTransport transport = new();
        platform.AddTransport(transport);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        var watcher1 = platform.CreateWatcher();
        await watcher1.Start(TestContext.Current.CancellationToken);
        await watcher1.Start(TestContext.Current.CancellationToken);

        var watcher2 = platform.CreateWatcher();
        await watcher2.Start(TestContext.Current.CancellationToken);
        await watcher2.Start(TestContext.Current.CancellationToken);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(1, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await watcher1.Stop(TestContext.Current.CancellationToken);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(1, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await watcher2.Stop(TestContext.Current.CancellationToken);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(1, transport.StartDiscoveryCalled);
        Assert.Equal(1, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);
    }

    [Fact]
    public async Task SingleWatcher_ShouldStopOnce_WhenDisposed()
    {
        var platform = CreatePlatform("TestDevice");

        SpyDiscoveryTransport transport = new();
        platform.AddTransport(transport);

        var watcher = platform.CreateWatcher();

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await watcher.Start(TestContext.Current.CancellationToken);
        await watcher.Start(TestContext.Current.CancellationToken);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(1, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await watcher.DisposeAsync();
        await watcher.DisposeAsync();

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(1, transport.StartDiscoveryCalled);
        Assert.Equal(1, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);
    }

    [Fact]
    public async Task SingleAdvertiser_ShouldStartOnce()
    {
        var platform = CreatePlatform("TestDevice");

        SpyDiscoveryTransport transport = new();
        platform.AddTransport(transport);

        var advertiser = platform.CreateAdvertiser();

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await advertiser.Start(TestContext.Current.CancellationToken);
        await advertiser.Start(TestContext.Current.CancellationToken);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(1, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await advertiser.Stop(TestContext.Current.CancellationToken);
        await advertiser.Stop(TestContext.Current.CancellationToken);
        await advertiser.DisposeAsync();

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(1, transport.StartAdvertisementCalled);
        Assert.Equal(1, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);
    }

    [Fact]
    public async Task MultipleAdvertiser_ShouldStartOnce()
    {
        var platform = CreatePlatform("TestDevice");

        SpyDiscoveryTransport transport = new();
        platform.AddTransport(transport);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        var advertiser1 = platform.CreateAdvertiser();
        await advertiser1.Start(TestContext.Current.CancellationToken);
        await advertiser1.Start(TestContext.Current.CancellationToken);

        var advertiser2 = platform.CreateAdvertiser();
        await advertiser2.Start(TestContext.Current.CancellationToken);
        await advertiser2.Start(TestContext.Current.CancellationToken);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(1, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await advertiser1.Stop(TestContext.Current.CancellationToken);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(1, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await advertiser2.Stop(TestContext.Current.CancellationToken);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(1, transport.StartAdvertisementCalled);
        Assert.Equal(1, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);
    }

    [Fact]
    public async Task SingleAdvertiser_ShouldStopOnce_WhenDisposed()
    {
        var platform = CreatePlatform("TestDevice");

        SpyDiscoveryTransport transport = new();
        platform.AddTransport(transport);

        var advertiser = platform.CreateAdvertiser();

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await advertiser.Start(TestContext.Current.CancellationToken);
        await advertiser.Start(TestContext.Current.CancellationToken);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(1, transport.StartAdvertisementCalled);
        Assert.Equal(0, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await advertiser.DisposeAsync();
        await advertiser.DisposeAsync();

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(1, transport.StartAdvertisementCalled);
        Assert.Equal(1, transport.StopAdvertisementCalled);
        Assert.Equal(0, transport.StartDiscoveryCalled);
        Assert.Equal(0, transport.StopDiscoveryCalled);
        Assert.Equal(0, transport.DisposeCalled);
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotStopListen_WhenNotInitialized()
    {
        var platform = CreatePlatform("TestDevice");

        SpyTransport transport = new();
        platform.AddTransport(transport);

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(0, transport.DisposeCalled);

        await platform.DisposeAsync();

        Assert.Equal(0, transport.StartListenCalled);
        Assert.Equal(0, transport.StopListenCalled);
        Assert.Equal(1, transport.DisposeCalled);
    }

    class SpyTransport : ICdpTransport
    {
        public CdpTransportType TransportType { get; } = CdpTransportType.Unknown;

        public event DeviceConnectedEventHandler? DeviceConnected;

        public Task<CdpSocket> ConnectAsync(EndpointInfo endpoint, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public EndpointInfo GetEndpoint()
            => new(CdpTransportType.Unknown, "Address", "SomeService");

        public int StartListenCalled { get; private set; }
        public ValueTask StartListen(CancellationToken cancellation)
        {
            StartListenCalled++;
            return ValueTask.CompletedTask;
        }

        public int StopListenCalled { get; private set; }
        public ValueTask StopListen(CancellationToken cancellation)
        {
            StopListenCalled++;
            return ValueTask.CompletedTask;
        }

        public int DisposeCalled { get; private set; }
        public void Dispose()
           => DisposeCalled++;
    }

    sealed class SpyDiscoveryTransport : SpyTransport, ICdpDiscoverableTransport
    {
        public event DeviceDiscoveredEventHandler? DeviceDiscovered;

        public int StartAdvertisementCalled { get; private set; }
        public ValueTask StartAdvertisement(LocalDeviceInfo deviceInfo, CancellationToken cancellationToken)
        {
            StartAdvertisementCalled++;
            return ValueTask.CompletedTask;
        }

        public int StopAdvertisementCalled { get; private set; }
        public ValueTask StopAdvertisement(CancellationToken cancellationToken)
        {
            StopAdvertisementCalled++;
            return ValueTask.CompletedTask;
        }

        public int StartDiscoveryCalled { get; private set; }
        public ValueTask StartDiscovery(CancellationToken cancellationToken)
        {
            StartDiscoveryCalled++;
            return ValueTask.CompletedTask;
        }

        public int StopDiscoveryCalled { get; private set; }
        public ValueTask StopDiscovery(CancellationToken cancellationToken)
        {
            StopDiscoveryCalled++;
            return ValueTask.CompletedTask;
        }
    }
}
