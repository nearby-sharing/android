using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Internal;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Messages;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
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

        Guid operationId = Guid.NewGuid();

        HandshakeHandler handshake = new();
        using (var handShakeChannel = await session.StartClientChannelAsync(NearShareHandshakeApp.Id, NearShareHandshakeApp.Name, handshake))
        {
            HandshakeHandler.StartHandshake(handShakeChannel, operationId);
            await handshake;
        }

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
        readonly TaskCompletionSource _promise = new();

        public static void StartHandshake(CdpChannel channel, Guid operationId)
        {
            ValueSet msg = new();
            msg.Add("ControlMessage", (uint)NearShareControlMsgType.HandShakeRequest);
            msg.Add("MaxPlatformVersion", 1u);
            msg.Add("MinPlatformVersion", 1u);
            msg.Add("OperationId", operationId);
            channel.SendMessage(msg.Write);
        }

        public ValueTask HandleMessageAsync(CdpMessage msg)
        {
            throw new NotImplementedException();
        }

        public TaskAwaiter GetAwaiter()
            => _promise.Task.GetAwaiter();
    }

    class SenderStateMachine : IChannelMessageHandler
    {
        public ValueTask HandleMessageAsync(CdpMessage msg)
        {
            throw new NotImplementedException();
        }
    }
}
