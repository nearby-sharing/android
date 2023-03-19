using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
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
            SendValueSet(msg, msgId: 0);

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
            SendValueSet(valueSet, 10);

            await _promise.Task;
        }

        CdpFileProvider[]? _files;
        IProgress<NearShareProgress>? _fileProgress;
        CancellationToken? _fileCancellationToken;
        ulong _bytesToSend;
        public async Task SendFilesAsync(CdpFileProvider[] files, IProgress<NearShareProgress> progress, CancellationToken cancellationToken)
        {
            _files = files;
            _fileProgress = progress;
            _fileCancellationToken = cancellationToken;

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
            SendValueSet(valueSet, 10);

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
            if (_fileCancellationToken?.IsCancellationRequested == true)
                return;

            var payload = ValueSet.Parse(msg.ReadBinary(out var header));
            try
            {
                var controlMsg = (NearShareControlMsgType)payload.Get<uint>("ControlMessage");
                switch (controlMsg)
                {
                    case NearShareControlMsgType.FetchDataRequest:
                        HandleDataRequest(header, payload);
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

        ulong _bytesSent = 0;
        void HandleDataRequest(BinaryMsgHeader header, ValueSet payload)
        {
            var contentId = payload.Get<uint>("ContentId");
            var start = payload.Get<ulong>("BlobPosition");
            var length = payload.Get<uint>("BlobSize");

            var fileProvider = _files?[contentId] ?? throw new NullReferenceException("Could not access files to transfer");
            var blob = fileProvider.ReadBlob(start, length);

            _fileProgress?.Report(new()
            {
                BytesSent = Interlocked.Add(ref _bytesSent, length),
                FilesSent = contentId + 1, // ToDo: How to calculate?
                TotalBytesToSend = _bytesToSend,
                TotalFilesToSend = (uint)_files.Length
            });

            ValueSet response = new();
            response.Add("ControlMessage", (uint)NearShareControlMsgType.FetchDataResponse);
            response.Add("ContentId", contentId);
            response.Add("BlobPosition", start);
            response.Add("DataBlob", blob.ToArray().ToList()); // ToDo: Remove allocation
            SendValueSet(response, header.MessageId);
        }
    }
}
