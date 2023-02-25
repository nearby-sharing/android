using Microsoft.CorrelationVector;
using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Internal;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Messages;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Serialization;

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
        using var handShakeChannel = await session.StartClientChannelAsync(NearShareHandshakeApp.Id, NearShareHandshakeApp.Name, handshake);
        var handshakeResultMsg = await handshake.Execute(handShakeChannel, operationId);

        var cv = handshakeResultMsg.Header.TryGetCorrelationVector() ?? throw new InvalidDataException("No Correlation Vector");

        SenderStateMachine senderStateMachine = new();
        using var channel = await session.StartClientChannelAsync(operationId.ToString("D").ToUpper(), NearShareApp.Name, senderStateMachine, handShakeChannel.Socket);
        SenderStateMachine.SendUri(channel, uri, cv);
    }

    public Task SendFileAsync(CdpDevice device, CdpFileProvider file, IProgress<NearShareProgress> progress, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task SendFilesAsync(CdpDevice device, CdpFileProvider[] files, IProgress<NearShareProgress> progress, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    sealed class HandshakeHandler : IChannelMessageHandler
    {
        readonly TaskCompletionSource<CdpMessage> _promise = new();

        public Task<CdpMessage> Execute(CdpChannel channel, Guid operationId)
        {
            ValueSet msg = new();
            msg.Add("ControlMessage", (uint)NearShareControlMsgType.HandShakeRequest);
            msg.Add("MaxPlatformVersion", 1u);
            msg.Add("MinPlatformVersion", 1u);
            msg.Add("OperationId", operationId);
            channel.SendBinaryMessage(msg.Write, msgId: 0, new()
            {
                AdditionalHeader.CreateCorrelationHeader() // "CDPSvc" crashes if not supplied (AccessViolation in ShareHost.dll!ExtendCorrelationVector)
            });

            return _promise.Task;
        }

        public void HandleMessage(CdpMessage msg)
        {
            var payload = ValueSet.Parse(msg.ReadBinary(out _));
            var handshakeResult = payload.Get<uint>("VersionHandShakeResult");

            if (handshakeResult != 1)
                _promise.SetException(new CdpProtocolException("Handshake failed"));

            _promise.SetResult(msg);
        }
    }

    sealed class SenderStateMachine : IChannelMessageHandler
    {
        public static void SendUri(CdpChannel channel, Uri uri, CorrelationVector cv)
        {
            ValueSet valueSet = new();
            valueSet.Add("ControlMessage", (uint)NearShareControlMsgType.StartTransfer);
            valueSet.Add("DataKind", (uint)DataKind.Uri);
            valueSet.Add("BytesToSend", 0);
            valueSet.Add("FileCount", 0);
            valueSet.Add("Uri", uri.ToString());
            channel.SendBinaryMessage(valueSet.Write, 10, new()
            {
                AdditionalHeader.FromCorrelationVector(cv.Increment())
            });
        }

        public void HandleMessage(CdpMessage msg)
        {
            var payload = ValueSet.Parse(msg.ReadBinary(out _));
        }
    }
}
