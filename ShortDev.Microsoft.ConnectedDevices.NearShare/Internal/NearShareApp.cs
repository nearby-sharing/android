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

                    _logger.LogInformation("Receiving file \"{fileNames}\" from session {sessionId:X} via {transportType}",
                        string.Join(", ", fileNames),
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
                        DeviceName = Channel.Session.Device.Name,
                        TotalBytesToSend = bytesToSend,
                        TotalFilesToSend = (uint)fileNames.Count,
                        Files = files
                    };
                    HandleFileTransferToken(_fileTransferToken);

                    PlatformHandler.OnFileTransfer(_fileTransferToken);
                    return;
                }
            case DataKind.Uri:
                {
                    var uri = payload.Get<string>("Uri");
                    _logger.LogInformation("Received uri \"{uri}\" from session {sessionId:X}",
                        uri,
                        msg.Header.SessionId
                    );
                    PlatformHandler.OnReceivedUri(new()
                    {
                        DeviceName = Channel.Session.Device.Name,
                        Uri = uri
                    });

                    OnCompleted();
                    return;
                }
        }
        throw new NotImplementedException($"DataKind {dataKind} not implemented");
    }

    IEnumerator? _blobCursor;
    async void HandleFileTransferToken(FileTransferToken token)
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

    void HandleFetchDataResponse(ValueSet payload)
    {
        if (_fileTransferToken == null)
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
