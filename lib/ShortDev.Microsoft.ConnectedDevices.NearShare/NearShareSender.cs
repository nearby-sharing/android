using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Apps;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Messages;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Buffers;
using System.Diagnostics;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public sealed class NearShareSender(ConnectedDevicesPlatform platform)
{
    public ConnectedDevicesPlatform Platform { get; } = platform;

    public event EventHandler<CdpTransportType>? TransportUpgraded;

    async Task<SenderStateMachine> PrepareTransferInternalAsync(EndpointInfo endpoint, CancellationToken cancellationToken)
    {
        var session = await Platform.ConnectAsync(endpoint, options: new() { TransportUpgraded = TransportUpgraded }, cancellationToken);

        Guid operationId = Guid.NewGuid();

        HandshakeHandler handshake = new(Platform);
        using var handShakeChannel = await session.StartClientChannelAsync(handshake, cancellationToken);
        var handshakeResultMsg = await handshake.Execute(operationId);

        // ToDo: CorrelationVector
        // var cv = handshakeResultMsg.Header.TryGetCorrelationVector() ?? throw new InvalidDataException("No Correlation Vector");

        SenderStateMachine senderStateMachine = new(Platform);
        var channel = await session.StartClientChannelAsync(operationId.ToString("D").ToUpper(), NearShareApp.Name, senderStateMachine, cancellationToken);
        return senderStateMachine;
    }

    public async Task SendUriAsync(CdpDevice device, Uri uri, CancellationToken cancellationToken = default)
    {
        using var senderStateMachine = await PrepareTransferInternalAsync(device.Endpoint, cancellationToken);
        await senderStateMachine.SendUriAsync(uri);
    }

    public async Task SendFileAsync(CdpDevice device, CdpFileProvider file, IProgress<NearShareProgress> progress, CancellationToken cancellationToken = default)
        => await SendFilesAsync(device, [file], progress, cancellationToken);

    public async Task SendFilesAsync(CdpDevice device, IReadOnlyList<CdpFileProvider> files, IProgress<NearShareProgress> progress, CancellationToken cancellationToken = default)
    {
        using var senderStateMachine = await PrepareTransferInternalAsync(device.Endpoint, cancellationToken);
        await senderStateMachine.SendFilesAsync(files, progress, cancellationToken);
    }

    sealed class HandshakeHandler(ConnectedDevicesPlatform cdp) : CdpAppBase(cdp), ICdpAppId
    {
        public static string Id { get; } = NearShareHandshakeApp.Id;
        public static string Name { get; } = NearShareHandshakeApp.Name;

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
            msg.ReadBinary(out ValueSet payload, out _);
            var handshakeResult = payload.Get<uint>("VersionHandShakeResult");

            if (handshakeResult != 1)
                _promise.SetException(new CdpProtocolException("Handshake failed"));

            _promise.SetResult(msg);
        }
    }

    sealed class SenderStateMachine(ConnectedDevicesPlatform cdp) : CdpAppBase(cdp)
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

        IReadOnlyList<CdpFileProvider>? _files;
        IProgress<NearShareProgress>? _fileProgress;
        CancellationToken? _fileCancellationToken;
        ulong _bytesToSend;
        public async Task SendFilesAsync(IReadOnlyList<CdpFileProvider> files, IProgress<NearShareProgress> progress, CancellationToken cancellationToken)
        {
            _files = files;
            _fileProgress = progress;
            _fileCancellationToken = cancellationToken;

            uint fileCount = (uint)files.Count;
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

        static ulong CalcBytesToSend(IReadOnlyList<CdpFileProvider> files)
        {
            ulong sum = 0;
            for (int i = 0; i < files.Count; i++)
                sum += files[i].FileSize;
            return sum;
        }

        public override void HandleMessage(CdpMessage msg)
        {
            if (_fileCancellationToken?.IsCancellationRequested == true)
                return;

            msg.ReadBinary(out ValueSet payload, out var header);
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

            var fileProvider = _files?[(int)contentId] ?? throw new NullReferenceException("Could not access files to transfer");
            Channel.SendBinaryMessage(writer =>
            {
                FetchDataResponse.Write(writer, contentId, start, (int)length, out var blob);
                Debug.Assert(blob.Length == length);

                fileProvider.ReadBlob(start, blob);
            }, header.MessageId);

            _fileProgress?.Report(new()
            {
                TransferedBytes = Interlocked.Add(ref _bytesSent, length),
                TotalBytes = _bytesToSend,
                TotalFiles = (uint)_files.Count
            });
        }
    }
}
