using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Messages;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using System.Collections;
using System.Runtime.InteropServices;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare.Apps;

internal sealed class NearShareApp(ConnectedDevicesPlatform cdp) : CdpAppBase
{
    const uint PartitionSize = 102400u; // 131072u
    public static string Name { get; } = "NearSharePlatform";

    readonly ILogger<NearShareApp> _logger = cdp.CreateLogger<NearShareApp>();

    public required string Id { get; init; }
    public required NearShareReceiver Receiver { get; init; }

    uint _messageId = 0;
    public override void HandleMessage(CdpMessage msg)
    {
        msg.ReadBinary(out ValueSet payload, out var binaryHeader);
        _messageId = binaryHeader.MessageId;

        if (!payload.ContainsKey("ControlMessage"))
            throw new InvalidDataException();

        var msgType = (NearShareControlMsgType)payload.Get<uint>("ControlMessage");
        switch (msgType)
        {
            case NearShareControlMsgType.StartTransfer:
                HandleStartTransfer(msg, payload);
                return;
            case NearShareControlMsgType.FetchDataResponse:
                HandleFetchDataResponse(payload);
                return;
        }
        throw CdpSession.UnexpectedMessage(msgType.ToString());
    }

    FileTransferToken? _fileTransferToken;
    void HandleStartTransfer(CdpMessage msg, ValueSet payload)
    {
        var dataKind = (DataKind)payload.Get<uint>("DataKind");
        switch (dataKind)
        {
            case DataKind.File:
                {
                    var fileNames = payload.Get<List<string>>("FileNames");

                    _logger.ReceivingFile(
                        fileNames,
                        msg.Header.SessionId,
                        Channel.Socket.TransportType
                    );

                    var bytesToSend = payload.Get<ulong>("BytesToSend");
                    var contentIds = payload.Get<IList<uint>>("ContentIds");
                    var contentSizes = payload.Get<IList<ulong>>("ContentSizes");

                    var files = new FileShareInfo[fileNames.Count];
                    for (int i = 0; i < files.Length; i++)
                    {
                        files[i] = new(contentIds[i], fileNames[i], contentSizes[i]);
                    }
                    _fileTransferToken = new()
                    {
                        DeviceName = Channel.Session.DeviceInfo?.Name ?? "UNKNOWM",
                        TotalBytes = bytesToSend,
                        Files = files
                    };
                    _fileTransferToken.CancellationToken.Register(OnCancel);
                    _fileTransferToken.Accepted += OnFileTransferAccepted;

                    Receiver.OnFileTransfer(_fileTransferToken);
                    return;
                }
            case DataKind.Uri:
                {
                    var uri = payload.Get<string>("Uri");

                    _logger.ReceivedUrl(
                        uri,
                        msg.Header.SessionId,
                        Channel.Socket.TransportType
                    );

                    Receiver.OnReceivedUri(new()
                    {
                        DeviceName = Channel.Session.DeviceInfo?.Name ?? "UNKNOWM",
                        Uri = uri
                    });

                    OnCompleted();
                    return;
                }
        }
        throw new NotImplementedException($"DataKind {dataKind} not implemented");
    }

    IEnumerator? _blobCursor;
    void OnFileTransferAccepted(FileTransferToken token)
    {
        _blobCursor = CreateBlobCursor(token);
        _blobCursor.MoveNext();

        IEnumerator CreateBlobCursor(FileTransferToken transferToken)
        {
            foreach (var file in transferToken)
            {
                var contentId = file.Id;
                var bytesToSend = file.Size;

                ulong requestedPosition = 0;
                for (; requestedPosition + PartitionSize < bytesToSend; requestedPosition += PartitionSize)
                {
                    RequestBlob(requestedPosition, contentId);
                    yield return null;
                }
                RequestBlob(requestedPosition, contentId, (uint)(bytesToSend - requestedPosition));
            }

            void RequestBlob(ulong requestedPosition, uint contentId, uint size = PartitionSize)
            {
                ValueSet request = new();
                request.Add("ControlMessage", (uint)NearShareControlMsgType.FetchDataRequest);
                request.Add("BlobPosition", requestedPosition);
                request.Add("BlobSize", size);
                request.Add("ContentId", contentId);
                SendValueSet(request, _messageId);
            }
        }
    }

    void HandleFetchDataResponse(ValueSet payload)
    {
        if (_fileTransferToken is null || _blobCursor is null)
            throw new CdpProtocolException("FileTransfer has not been initialized");

        var contentId = payload.Get<uint>("ContentId");
        var position = payload.Get<ulong>("BlobPosition");
        var blob = payload.Get<List<byte>>("DataBlob");
        var blobSize = (ulong)blob.Count;

        if (blobSize > PartitionSize) // ToDo: position > _bytesToSend
            throw new CdpSecurityException("Device tried to send too much data!");

        if (_fileTransferToken.CancellationToken.IsCancellationRequested)
        {
            OnCancel();
            return;
        }

        lock (_fileTransferToken)
        {
            var stream = _fileTransferToken.GetStream(contentId);
            stream.Position = (long)position;
            stream.Write(CollectionsMarshal.AsSpan(blob));
        }
        _fileTransferToken.SendProgressEvent(blobSize);

        if (_fileTransferToken.IsTransferComplete)
        {
            OnCompleted();
            return;
        }

        _blobCursor.MoveNext();
    }

    void OnCancel()
    {
        try
        {
            try
            {
                ValueSet request = new();
                request.Add("ControlMessage", (uint)NearShareControlMsgType.CancelTransfer);
                SendValueSet(request, _messageId);
            }
            finally
            {
                _fileTransferToken?.OnFinish();
            }
        }
        finally
        {
            Dispose();
        }
    }

    void OnCompleted()
    {
        try
        {
            try
            {
                ValueSet request = new();
                request.Add("ControlMessage", (uint)NearShareControlMsgType.CompleteTransfer);
                SendValueSet(request, _messageId);
            }
            finally
            {
                _fileTransferToken?.OnFinish();
            }
        }
        finally
        {
            Dispose();
        }
    }

    public override void Dispose()
    {
        cdp.TryUnregisterApp(Id);

        base.Dispose();
    }
}
