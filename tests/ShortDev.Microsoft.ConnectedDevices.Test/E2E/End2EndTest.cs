using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;
using ShortDev.Microsoft.ConnectedDevices.Transports.Network;
using System.Net;
using Xunit.Abstractions;

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
        TestUtils.ListenLocalOnly();

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

        using var device1 = CreateDevice(network, "Device 1", "57-0C-4A-27-07-52");
        if (useTcp1)
            UseTcp(device1, tcpPort: 5041, udpPort: 5051);

        device1.Discover(cancellationToken: default);

        using var device2 = CreateDevice(network, "Device 2", "81-7A-80-8F-D5-80");
        if (useTcp2)
            UseTcp(device2, tcpPort: 5041, udpPort: 5051);

        device2.Advertise(cancellationToken: default);
        device2.Listen(cancellationToken: default);

        TaskCompletionSource<UriTransferToken> receivePromise = new();
        NearShareReceiver.ReceivedUri += receivePromise.SetResult;
        NearShareReceiver.Register(device2);

        try
        {
            NearShareSender sender = new(device1);
            await sender.SendUriAsync(
                device: new("Device 2", DeviceType.Linux, Endpoint:
                    new(Transports.CdpTransportType.Rfcomm, "81-7A-80-8F-D5-80", "ServiceId")
                ), new Uri("https://nearshare.shortdev.de/")
            );

            var token = await receivePromise.Task;
            Assert.Equal("Device 1", token.DeviceName);
            Assert.Equal("https://nearshare.shortdev.de/", token.Uri);
        }
        finally
        {
            NearShareReceiver.Unregister();
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task TransferFile(bool useTcp1, bool useTcp2)
    {
        DeviceContainer network = new();

        using var device1 = CreateDevice(network, "Device 1", "57-0C-4A-27-07-52");
        if (useTcp1)
            UseTcp(device1, tcpPort: 5041, udpPort: 5051);

        device1.Discover(cancellationToken: default);

        using var device2 = CreateDevice(network, "Device 2", "81-7A-80-8F-D5-80");
        if (useTcp2)
            UseTcp(device2, tcpPort: 5041, udpPort: 5051);

        device2.Advertise(cancellationToken: default);
        device2.Listen(cancellationToken: default);

        var buffer = new byte[Random.Shared.Next(1_000, 1_000_000)];
        outputHelper.WriteLine($"[Information]: Generated buffer with size {buffer.LongLength}");
        Random.Shared.NextBytes(buffer);

        MemoryStream receivedData = new();
        TaskCompletionSource receivePromise = new();
        NearShareReceiver.FileTransfer += OnFileTransfer;
        NearShareReceiver.Register(device2);

        try
        {
            NearShareSender sender = new(device1);
            await sender.SendFileAsync(
                device: new("Device 2", DeviceType.Linux, Endpoint:
                    new(Transports.CdpTransportType.Rfcomm, "81-7A-80-8F-D5-80", "ServiceId")
                ),
                CdpFileProvider.FromBuffer("TestFile", buffer),
                new Progress<NearShareProgress>()
            );

            await receivePromise.Task;

            Assert.Equal(buffer, receivedData.ToArray());
        }
        finally
        {
            NearShareReceiver.Unregister();
        }

        void OnFileTransfer(FileTransferToken token)
        {
            Assert.Equal("Device 1", token.DeviceName);
            Assert.Equal(1, token.Files.Count);
            Assert.Equal("TestFile", token.Files[0].Name);
            Assert.Equal((ulong)buffer.LongLength, token.Files[0].Size);

            token.Accept([receivedData]);
            token.Finished += receivePromise.SetResult;
        }
    }
}
