using ShortDev.Microsoft.ConnectedDevices.AppControl.Messages;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Runtime.CompilerServices;

namespace ShortDev.Microsoft.ConnectedDevices.AppControl;

public sealed class AppControlClient(ConnectedDevicesPlatform platform)
{
    public ConnectedDevicesPlatform Platform { get; } = platform;

    public event EventHandler<CdpTransportType>? TransportUpgraded;

    public async Task<CallAppServiceResponse> CallAppService(EndpointInfo endpoint, string packageName, string appService, ReadOnlyMemory<byte> data, InputMessageFormat dataFormat, CancellationToken cancellationToken = default)
    {
        var session = await Platform.ConnectAsync(endpoint, options: new() { TransportUpgraded = TransportUpgraded }, cancellationToken).ConfigureAwait(false);

        var handler = await session.StartClientChannelAsync<AppServiceHandler>(cancellationToken).ConfigureAwait(false);

        handler.SendRequest(new CallAppServiceRequest()
        {
            PackageName = packageName,
            AppServiceName = appService,
            Data = data,
            DataFormat = dataFormat,
        });

        return await handler;
    }

    sealed class AppServiceHandler(CdpChannel channel) : SystemAppControlApp(channel), ICdpAppFactory<AppServiceHandler>
    {
        readonly TaskCompletionSource<CallAppServiceResponse> _promise = new();

        public void SendRequest(in CallAppServiceRequest request)
            => SendAppControlMessage(AppControlType.CallAppService, request);

        protected override void OnCallAppServiceResponse(in CallAppServiceResponse response)
            => _promise.TrySetResult(response);

        public TaskAwaiter<CallAppServiceResponse> GetAwaiter() => _promise.Task.GetAwaiter();

        static AppServiceHandler ICdpAppFactory<AppServiceHandler>.Create(CdpChannel channel) => new(channel);
    }
}
