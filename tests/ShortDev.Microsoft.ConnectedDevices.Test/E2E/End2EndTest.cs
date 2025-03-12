using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;
using ShortDev.Microsoft.ConnectedDevices.Transports.Network;
using System.Collections.Frozen;
using System.Net;

namespace ShortDev.Microsoft.ConnectedDevices.Test.E2E;

public sealed class End2EndTest(ITestOutputHelper outputHelper)
{
    ConnectedDevicesPlatform CreateDevice(DeviceContainer network, string name, string btAddress)
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
        ConnectedDevicesPlatform cdp = new(DeviceInfo, loggerFactory);

        BluetoothHandler btHandler = new(network, new(Transports.CdpTransportType.Rfcomm, btAddress));
        cdp.AddTransport(new BluetoothTransport(btHandler));

        return cdp;
    }

    static void UseTcp(ConnectedDevicesPlatform cdp, int tcpPort, int udpPort)
    {
        NetworkHandler networkHandler = new(IPAddress.Loopback);
        NetworkTransport networkTransport = new(networkHandler, tcpPort, udpPort);
        cdp.AddTransport(networkTransport);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task TransferUri(bool useTcp1, bool useTcp2)
    {
        DeviceContainer network = new();

        await using var device1 = CreateDevice(network, "Device 1", "57-0C-4A-27-07-52");
        if (useTcp1)
            UseTcp(device1, tcpPort: 5041, udpPort: 5051);

        await using var watcher = device1.CreateWatcher();
        await watcher.Start(TestContext.Current.CancellationToken);

        await using var device2 = CreateDevice(network, "Device 2", "81-7A-80-8F-D5-80");
        if (useTcp2)
            UseTcp(device2, tcpPort: 5041, udpPort: 5051);

        await device2.InitializeAsync(TestContext.Current.CancellationToken);
        await using var advertiser = device2.CreateAdvertiser();
        await advertiser.Start(TestContext.Current.CancellationToken);

        TaskCompletionSource<UriTransferToken> receivePromise = new();
        using NearShareReceiver receiver = new(device2);
        receiver.ReceivedUri += receivePromise.SetResult;

        NearShareSender sender = new(device1);
        await sender.SendUriAsync(
            device: new("Device 2", DeviceType.Linux, Endpoint:
                new(Transports.CdpTransportType.Rfcomm, "81-7A-80-8F-D5-80", "ServiceId")
            ), new Uri("https://nearshare.shortdev.de/"),
            TestContext.Current.CancellationToken
        );

        var token = await receivePromise.Task;
        Assert.Equal("Device 1", token.DeviceName);
        Assert.Equal("https://nearshare.shortdev.de/", token.Uri);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task TransferFile(bool useTcp1, bool useTcp2)
    {
        DeviceContainer network = new();

        await using var device1 = CreateDevice(network, "Device 1", "57-0C-4A-27-07-52");
        if (useTcp1)
            UseTcp(device1, tcpPort: 5041, udpPort: 5051);

        await using var watcher = device1.CreateWatcher();
        await watcher.Start(TestContext.Current.CancellationToken);

        await using var device2 = CreateDevice(network, "Device 2", "81-7A-80-8F-D5-80");
        if (useTcp2)
            UseTcp(device2, tcpPort: 5041, udpPort: 5051);

        await device2.InitializeAsync(TestContext.Current.CancellationToken);
        await using var advertiser = device2.CreateAdvertiser();
        await advertiser.Start(TestContext.Current.CancellationToken);

        var buffer = new byte[Random.Shared.Next(1_000, 1_000_000)];
        outputHelper.WriteLine($"[Information]: Generated buffer with size {buffer.LongLength}");
        Random.Shared.NextBytes(buffer);

        MemoryStream receivedData = new();
        TaskCompletionSource receivePromise = new();
        using NearShareReceiver receiver = new(device2);
        receiver.FileTransfer += OnFileTransfer;

        NearShareSender sender = new(device1);
        await sender.SendFileAsync(
            device: new("Device 2", DeviceType.Linux, Endpoint:
                new(Transports.CdpTransportType.Rfcomm, "81-7A-80-8F-D5-80", "ServiceId")
            ),
            CdpFileProvider.FromBuffer("TestFile", buffer),
            new Progress<NearShareProgress>(),
            TestContext.Current.CancellationToken
        );

        await receivePromise.Task;

        Assert.Equal(buffer, receivedData.ToArray());

        void OnFileTransfer(FileTransferToken token)
        {
            Assert.Equal("Device 1", token.DeviceName);
            Assert.Single(token.Files);
            Assert.Equal("TestFile", token.Files[0].Name);
            Assert.Equal((ulong)buffer.LongLength, token.Files[0].Size);

            token.Accept(new Dictionary<uint, Stream>() { { 0, receivedData } }.ToFrozenDictionary());
            token.Finished += receivePromise.SetResult;
        }
    }
}
