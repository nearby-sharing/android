using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages;
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

    async Task<SenderStateMachine> PrepareTransferInternalAsync(CdpDevice device)
    {
        var session = await Platform.ConnectAsync(device);

        Guid operationId = Guid.NewGuid();

        HandshakeHandler handshake = new();
        using var handShakeChannel = await session.StartClientChannelAsync(NearShareHandshakeApp.Id, NearShareHandshakeApp.Name, handshake);
        var handshakeResultMsg = await handshake.Execute(operationId);

        // ToDo: CorrelationVector
        // var cv = handshakeResultMsg.Header.TryGetCorrelationVector() ?? throw new InvalidDataException("No Correlation Vector");

        SenderStateMachine senderStateMachine = new();
        var channel = await session.StartClientChannelAsync(operationId.ToString("D").ToUpper(), NearShareApp.Name, senderStateMachine, handShakeChannel.Socket);
        return senderStateMachine;
    }

    void Dispose(CdpAppBase app)
    {
        app.Channel.Dispose(closeSession: true, closeSocket: true);
    }

    public async Task SendUriAsync(CdpDevice device, Uri uri)
    {
        var senderStateMachine = await PrepareTransferInternalAsync(device);
        await senderStateMachine.SendUriAsync(uri);
        Dispose(senderStateMachine);
    }

    public async Task SendFileAsync(CdpDevice device, CdpFileProvider file, IProgress<NearShareProgress> progress, CancellationToken cancellationToken = default)
        => await SendFilesAsync(device, new[] { file }, progress, cancellationToken);

    public async Task SendFilesAsync(CdpDevice device, CdpFileProvider[] files, IProgress<NearShareProgress> progress, CancellationToken cancellationToken = default)
    {
        var senderStateMachine = await PrepareTransferInternalAsync(device);
        await senderStateMachine.SendFilesAsync(files, progress, cancellationToken);
        Dispose(senderStateMachine);
    }

    sealed class HandshakeHandler : CdpAppBase
    {
        readonly TaskCompletionSource<CdpMessage> _promise = new();

        public Task<CdpMessage> Execute(Guid operationId)
        {
            ValueSet msg = new();
            msg.Add("ControlMessage", (uint)NearShareControlMsgType.HandShakeRequest);
            msg.Add("MaxPlatformVersion", 1u);
            msg.Add("MinPlatformVersion", 1u);
            msg.Add("OperationId", operationId);
            Channel.SendBinaryMessage(msg.Write, msgId: 0, new()
            {
                AdditionalHeader.CreateCorrelationHeader() // "CDPSvc" crashes if not supplied (AccessViolation in ShareHost.dll!ExtendCorrelationVector)
            });

            return _promise.Task;
        }

        public override void HandleMessage(CdpMessage msg)
        {
            var payload = ValueSet.Parse(msg.ReadBinary(out _));
            var handshakeResult = payload.Get<uint>("VersionHandShakeResult");

            if (handshakeResult != 1)
                _promise.SetException(new CdpProtocolException("Handshake failed"));

            _promise.SetResult(msg);
        }
    }

    sealed class SenderStateMachine : CdpAppBase
    {
        readonly TaskCompletionSource _promise = new();
        public async Task SendUriAsync(Uri uri)
        {
            ValueSet valueSet = new();
            valueSet.Add("ControlMessage", (uint)NearShareControlMsgType.StartTransfer);
            valueSet.Add("DataKind", (uint)DataKind.Uri);
            valueSet.Add("BytesToSend", 0);
            valueSet.Add("FileCount", 0);
            valueSet.Add("Uri", uri.ToString());
            Channel.SendBinaryMessage(valueSet.Write, 10, new()
            {
                AdditionalHeader.CreateCorrelationHeader()
            });

            await _promise.Task;
        }

        CdpFileProvider[] _files;
        IProgress<NearShareProgress> _fileProgress;
        CancellationToken _fileCancellationToken;
        ulong _bytesToSend;
        public async Task SendFilesAsync(CdpFileProvider[] files, IProgress<NearShareProgress> progress, CancellationToken cancellationToken)
        {
            _files = files;
            _fileProgress = progress;

            uint fileCount = (uint)files.Length;
            _bytesToSend = CalcBytesToSend(files);

            ValueSet valueSet = new();
            valueSet.Add("ControlMessage", (uint)NearShareControlMsgType.StartTransfer);
            valueSet.Add("DataKind", (uint)DataKind.File);
            valueSet.Add<ulong>("BytesToSend", _bytesToSend);
            valueSet.Add<uint>("FileCount", fileCount);
            valueSet.Add<uint[]>("ContentIds", GenerateContentIds(fileCount));
            valueSet.Add<ulong[]>("ContentSizes", files.Select(x => x.FileSize).ToArray());
            valueSet.Add<string[]>("FileNames", files.Select(x => x.FileName).ToArray());
            Channel.SendBinaryMessage(valueSet.Write, 10, new()
            {
                AdditionalHeader.CreateCorrelationHeader()
            });

            cancellationToken.Register(() =>
            {
                ValueSet request = new();
                request.Add("ControlMessage", (uint)NearShareControlMsgType.CancelTransfer);
                SendValueSet(request, 11);
                _promise.TrySetCanceled();
            });

            await _promise.Task;
        }

        static uint[] GenerateContentIds(uint fileCount)
        {
            var ids = new uint[fileCount];
            for (uint i = 0; i < fileCount; i++)
                ids[i] = i;
            return ids;
        }

        static ulong CalcBytesToSend(CdpFileProvider[] files)
        {
            ulong sum = 0;
            for (int i = 0; i < files.Length; i++)
                sum += files[i].FileSize;
            return sum;
        }

        public override void HandleMessage(CdpMessage msg)
        {
            var payload = ValueSet.Parse(msg.ReadBinary(out _));
            try
            {
                var controlMsg = (NearShareControlMsgType)payload.Get<uint>("ControlMessage");
                switch (controlMsg)
                {
                    case NearShareControlMsgType.FetchDataRequest:
                        HandleDataRequest(payload);
                        break;
                    case NearShareControlMsgType.CompleteTransfer:
                        _promise.TrySetResult();
                        break;
                    case NearShareControlMsgType.CancelTransfer:
                        _promise.TrySetCanceled();
                        break;
                    default:
                        throw new CdpProtocolException($"Unexpected {controlMsg}");
                }
            }
            catch (Exception ex)
            {
                _promise.TrySetException(ex);
            }
        }

        void HandleDataRequest(ValueSet payload)
        {

        }
    }
}
