using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Internal;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using System.Runtime.CompilerServices;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public sealed class NearShareSender
{
    public ConnectedDevicesPlatform Platform { get; }
    public NearShareSender(ConnectedDevicesPlatform platform)
    {
        Platform = platform;
    }

    public async Task SendUriAsync(CdpDevice device, Uri uri)
    {
        using var session = await Platform.ConnectAsync(device);

        HandshakeHandler handshake = new();
        using (var handShakeChannel = await session.StartClientChannelAsync(NearShareHandshakeApp.Id, NearShareHandshakeApp.Name, handshake))
            await handshake;

        SenderStateMachine senderStateMachine = new();
        using (var channel = await session.StartClientChannelAsync(NearShareHandshakeApp.Id, NearShareHandshakeApp.Name, senderStateMachine))
        {

        }
    }

    public Task SendFileAsync(CdpDevice device, CdpFileProvider file, IProgress<NearShareProgress> progress, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task SendFilesAsync(CdpDevice device, CdpFileProvider[] files, IProgress<NearShareProgress> progress, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    class HandshakeHandler : IChannelMessageHandler
    {
        public ValueTask HandleMessageAsync(CdpMessage msg)
        {
            throw new NotImplementedException();
        }

        public TaskAwaiter GetAwaiter()
            => throw new NotImplementedException();
    }

    class SenderStateMachine : IChannelMessageHandler
    {
        public ValueTask HandleMessageAsync(CdpMessage msg)
        {
            throw new NotImplementedException();
        }
    }
}
