using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Apps;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Messages;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Diagnostics;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

public sealed class NearShareSender(ConnectedDevicesPlatform platform)
{
    public ConnectedDevicesPlatform Platform { get; } = platform;

    public event EventHandler<CdpTransportType>? TransportUpgraded;

    async Task<SenderStateMachine> PrepareTransferInternalAsync(EndpointInfo endpoint, CancellationToken cancellationToken)
    {
        var session = await Platform.ConnectAsync(endpoint, options: new() { TransportUpgraded = TransportUpgraded }, cancellationToken).ConfigureAwait(false);

        Guid operationId = Guid.NewGuid();

        var handshake = await session.StartClientChannelAsync<HandshakeHandler>(cancellationToken).ConfigureAwait(false);
        await handshake.Execute(operationId).ConfigureAwait(false);

        // ToDo: CorrelationVector
        // var cv = handshakeResultMsg.Header.TryGetCorrelationVector() ?? throw new InvalidDataException("No Correlation Vector");

        var senderStateMachine = await session.StartClientChannelAsync<SenderStateMachine>(operationId.ToString("D").ToUpper(), NearShareApp.Name, cancellationToken).ConfigureAwait(false);
        return senderStateMachine;
    }

    public async Task SendUriAsync(CdpDevice device, Uri uri, CancellationToken cancellationToken = default)
    {
        using var senderStateMachine = await PrepareTransferInternalAsync(device.Endpoint, cancellationToken).ConfigureAwait(false);
        await senderStateMachine.SendUriAsync(uri).ConfigureAwait(false);
    }

    public async Task SendFileAsync(CdpDevice device, CdpFileProvider file, IProgress<NearShareProgress> progress, CancellationToken cancellationToken = default)
        => await SendFilesAsync(device, [file], progress, cancellationToken).ConfigureAwait(false);

    public async Task SendFilesAsync(CdpDevice device, IReadOnlyList<CdpFileProvider> files, IProgress<NearShareProgress> progress, CancellationToken cancellationToken = default)
    {
        using var senderStateMachine = await PrepareTransferInternalAsync(device.Endpoint, cancellationToken).ConfigureAwait(false);
        await senderStateMachine.SendFilesAsync(files, progress, cancellationToken).ConfigureAwait(false);
    }

    sealed class HandshakeHandler(CdpChannel channel) : CdpBondApp(channel), ICdpAppFactory<HandshakeHandler>, ICdpAppId
    {
        public static string Id { get; } = NearShareHandshakeApp.Id;
        public static string Name { get; } = NearShareHandshakeApp.Name;

        readonly TaskCompletionSource _promise = new();

        public Task Execute(Guid operationId)
        {
            ValueSet msg = new();
            msg.Add("ControlMessage", (uint)NearShareControlMsgType.HandShakeRequest);
            msg.Add("MaxPlatformVersion", 1u);
            msg.Add("MinPlatformVersion", 1u);
            msg.Add("OperationId", operationId);
            SendValueSet(msg, messageId: 0);

            return _promise.Task;
        }

        protected override void HandleMessage(in BinaryMsgHeader header, ValueSet payload)
        {
            var handshakeResult = payload.Get<uint>("VersionHandShakeResult");

            if (handshakeResult != 1)
                _promise.SetException(new CdpProtocolException("Handshake failed"));

            _promise.SetResult();
        }

        static HandshakeHandler ICdpAppFactory<HandshakeHandler>.Create(CdpChannel channel) => new(channel);
    }

    sealed class SenderStateMachine(CdpChannel channel) : CdpBondApp(channel), ICdpAppFactory<SenderStateMachine>
    {
        readonly TaskCompletionSource _promise = new();
        public async Task SendUriAsync(Uri uri)
        {
            ValueSet valueSet = new()
            {
                { "ControlMessage", (uint)NearShareControlMsgType.StartTransfer },
                { "DataKind", (uint)DataKind.Uri },
                { "BytesToSend", (ulong)0uL },
                { "FileCount", (uint)0u },
                { "Uri", uri.ToString(), Encoding.UTF8 }
            };
            SendValueSet(valueSet, 10);

            await _promise.Task.ConfigureAwait(false);
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

            ValueSet valueSet = new()
            {
                { "ControlMessage", (uint)NearShareControlMsgType.StartTransfer },
                { "DataKind", (uint)DataKind.File },
                { "BytesToSend", (ulong)_bytesToSend },
                { "FileCount", (uint)fileCount },
                { "ContentIds", GenerateContentIds(fileCount).ToList<uint>() },
                { "ContentSizes", files.Select(x => x.FileSize).ToList<ulong>() },
                { "FileNames", Encoding.UTF8, [.. files.Select(x => x.FileName)] }
            };
            SendValueSet(valueSet, 10);

            cancellationToken.Register(() =>
            {
                if (!_promise.TrySetCanceled())
                    return;

                ValueSet request = new()
                {
                    { "ControlMessage", (uint)NearShareControlMsgType.CancelTransfer }
                };
                SendValueSet(request, 11);
            });

            await _promise.Task.ConfigureAwait(false);
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

        protected override void HandleMessage(in BinaryMsgHeader header, ValueSet payload)
        {
            if (_fileCancellationToken?.IsCancellationRequested == true)
                return;

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
            SendBinaryMessage(new DataResponseMessage()
            {
                FileProvider = fileProvider,
                ContentId = contentId,
                Start = start,
                Length = length
            }, header.MessageId);

            _fileProgress?.Report(new()
            {
                TransferedBytes = Interlocked.Add(ref _bytesSent, length),
                TotalBytes = _bytesToSend,
                TotalFiles = (uint)_files.Count
            });
        }

        static SenderStateMachine ICdpAppFactory<SenderStateMachine>.Create(CdpChannel channel) => new(channel);

        private readonly struct DataResponseMessage : IBinaryWritable<DataResponseMessage>
        {
            public required CdpFileProvider FileProvider { get; init; }
            public required uint ContentId { get; init; }
            public required ulong Start { get; init; }
            public required uint Length { get; init; }

            // ToDo: Add the size of the rest of the message
            ulong IBinaryWritable<DataResponseMessage>.MinimumSize => Length;

            public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
            {
                FetchDataResponse.Write(ref writer, ContentId, Start, (int)Length, out var blob);
                Debug.Assert(blob.Length == Length);

                FileProvider.ReadBlob(Start, blob);
            }
        }

    }
}
