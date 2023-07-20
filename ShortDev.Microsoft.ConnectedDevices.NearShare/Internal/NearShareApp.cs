using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Messages;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare.Internal;

internal sealed class NearShareApp : CdpAppBase
{
    const uint PartitionSize = 102400u; // 131072u
    public static string Name { get; } = "NearSharePlatform";

    public required INearSharePlatformHandler PlatformHandler { get; init; }
    public required string Id { get; init; }

    [AllowNull] ILogger<NearShareApp> _logger;
    protected override void OnInitialized(CdpChannel channel)
    {
        _logger = channel.Session.Platform.DeviceInfo.LoggerFactory.CreateLogger<NearShareApp>();
    }

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
                HandleFetchDataResponse(msg, payload);
                return;
        }
        throw CdpSession.UnexpectedMessage(msgType.ToString());
    }

    FileTransferTokenImpl? _fileTransferToken;
    void HandleStartTransfer(CdpMessage msg, ValueSet payload)
    {
        var dataKind = (DataKind)payload.Get<uint>("DataKind");
        switch (dataKind)
        {
            case DataKind.File:
                {
                    var fileNames = payload.Get<List<string>>("FileNames");

                    _logger.LogInformation("Receiving file \"{0}\" from session {1:X} via {2}",
                        string.Join(", ", fileNames),
                        msg.Header.SessionId,
                        Channel.Socket.TransportType
                    );

                    var bytesToSend = payload.Get<ulong>("BytesToSend");
                    _fileTransferToken = new()
                    {
                        DeviceName = Channel.Session.Device.Name ?? "UNKNOWN",
                        FileNames = fileNames,
                        TotalBytesToSend = bytesToSend,
                        // Internal
                        ContentIds = payload.Get<IList<uint>>("ContentIds").ToArray(),
                        ContentSizes = payload.Get<IList<ulong>>("ContentSizes").ToArray(),
                        TotalFilesToSend = (uint)fileNames.Count
                    };
                    HandleFileTransferToken(_fileTransferToken);

                    PlatformHandler.OnFileTransfer(_fileTransferToken);
                    return;
                }
            case DataKind.Uri:
                {
                    var uri = payload.Get<string>("Uri");
                    _logger.LogInformation("Received uri \"{0}\" from session {1:X}",
                        uri,
                        msg.Header.SessionId
                    );
                    PlatformHandler.OnReceivedUri(new()
                    {
                        DeviceName = Channel.Session.Device.Name ?? "UNKNOWN",
                        Uri = uri
                    });

                    OnCompleted();
                    return;
                }
        }
        throw new NotImplementedException($"DataKind {dataKind} not implemented");
    }

    IEnumerator? _blobCursor;
    async void HandleFileTransferToken(FileTransferTokenImpl token)
    {
        try
        {
            await token.AwaitAcceptance();

            _blobCursor = CreateBlobCursor(token);
            _blobCursor.MoveNext();
        }
        catch (TaskCanceledException)
        {
            OnCancel();
        }

        IEnumerator CreateBlobCursor(FileTransferTokenImpl transferToken)
        {
            for (int i = 0; i < transferToken.ContentIds.Length; i++)
            {
                var contentId = transferToken.ContentIds[i];
                var bytesToSend = transferToken.ContentSizes[i];

                ulong requestedPosition = 0;
                for (; requestedPosition + PartitionSize < bytesToSend; requestedPosition += PartitionSize)
                {
                    RequestBlob(requestedPosition, contentId);
                    yield return null;
                }
                RequestBlob(requestedPosition, contentId, (uint)(bytesToSend - requestedPosition));

                transferToken.FilesSent++;
                transferToken.SendProgressEvent();
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

    void HandleFetchDataResponse(CdpMessage msg, ValueSet payload)
    {
        if (_fileTransferToken == null)
            throw new CdpProtocolException("FileTransfer has not been initialized");

        var contentId = payload.Get<uint>("ContentId");
        var position = payload.Get<ulong>("BlobPosition");
        var blob = payload.Get<List<byte>>("DataBlob");
        var blobSize = (ulong)blob.Count;

        if (blobSize > PartitionSize) // ToDo: position > _bytesToSend
            throw new CdpSecurityException("Device tried to send too much data!");

        // PlatformHandler.Log(0, $"BlobPosition: {position}; ({newPosition * 100 / bytesToSend}%)");
        lock (_fileTransferToken)
        {
            var stream = _fileTransferToken.GetStream(contentId);
            stream.Position = (long)position;
            stream.Write(CollectionsMarshal.AsSpan(blob));
        }

        _fileTransferToken.BytesSent += blobSize;
        _fileTransferToken.SendProgressEvent();

        var expectMessage = !_fileTransferToken.IsTransferComplete;
        if (expectMessage)
            _blobCursor?.MoveNext();
        else
        {
            OnCompleted();
            _fileTransferToken.Close();
        }
    }

    void OnCancel()
    {
        ValueSet request = new();
        request.Add("ControlMessage", (uint)NearShareControlMsgType.CancelTransfer);
        SendValueSet(request, _messageId);

        CloseChannel();
    }

    void OnCompleted()
    {
        ValueSet request = new();
        request.Add("ControlMessage", (uint)NearShareControlMsgType.CompleteTransfer);
        SendValueSet(request, _messageId);

        CloseChannel();
    }

    protected override void CloseChannel()
    {
        base.CloseChannel();
        CdpAppRegistration.TryUnregisterApp(Id);
    }
}
