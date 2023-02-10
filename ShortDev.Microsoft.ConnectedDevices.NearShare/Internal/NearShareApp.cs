using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Messages;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using System.Collections;
using System.Runtime.InteropServices;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare.Internal;

internal sealed class NearShareApp : CdpAppBase
{
    public static string Name { get; } = "NearSharePlatform";

    public required INearSharePlatformHandler PlatformHandler { get; init; }
    public required string Id { get; init; }

    const uint PartitionSize = 102400u; // 131072u

    uint _messageId = 0;
    public override void HandleMessage(CdpMessage msg)
    {
        var payload = ValueSet.Parse(msg.ReadBinary(out var binaryHeader));
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

    ulong transferedBytes = 0;
    ulong bytesToSend = 0;
    FileTransferToken? _fileTransferToken;
    void HandleStartTransfer(CdpMessage msg, ValueSet payload)
    {
        var dataKind = (DataKind)payload.Get<uint>("DataKind");
        switch (dataKind)
        {
            case DataKind.File:
                {
                    var fileNames = payload.Get<List<string>>("FileNames");
                    if (fileNames.Count != 1)
                        throw new NotImplementedException("Only able to receive one file at a time");

                    PlatformHandler.Log(0, $"Receiving file \"{fileNames[0]}\" from session {msg.Header.SessionId:X} via {Channel.Socket.TransportType}");

                    bytesToSend = payload.Get<ulong>("BytesToSend");

                    _fileTransferToken = new()
                    {
                        DeviceName = Channel.Session.Device.Name ?? "UNKNOWN",
                        FileName = fileNames[0],
                        FileSize = bytesToSend
                    };
                    HandleFileTransferToken(_fileTransferToken);

                    PlatformHandler.OnFileTransfer(_fileTransferToken);
                    return;
                }
            case DataKind.Uri:
                {
                    var uri = payload.Get<string>("Uri");
                    PlatformHandler.Log(0, $"Received uri \"{uri}\" from session {msg.Header.SessionId:X}");
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
    async void HandleFileTransferToken(FileTransferToken token)
    {
        try
        {
            await token.TaskInternal;

            _blobCursor = CreateBlobCursor();
            _blobCursor.MoveNext();
        }
        catch (TaskCanceledException)
        {
            OnCancel();
        }
    }

    void HandleFetchDataResponse(CdpMessage msg, ValueSet payload)
    {
        if (_fileTransferToken == null)
            throw new CdpProtocolException("FileTransfer has not been initialized");

        var position = payload.Get<ulong>("BlobPosition");
        var blob = payload.Get<List<byte>>("DataBlob");
        var blobSize = (ulong)blob.Count;

        if (position > bytesToSend || blobSize > PartitionSize)
            throw new CdpSecurityException("Device tried to send too much data!");

        // PlatformHandler.Log(0, $"BlobPosition: {position}; ({newPosition * 100 / bytesToSend}%)");
        lock (_fileTransferToken)
        {
            var stream = _fileTransferToken.Stream;
            stream.Position = (long)position;
            stream.Write(CollectionsMarshal.AsSpan(blob));
        }

        transferedBytes += blobSize;
        _fileTransferToken.ReceivedBytes = transferedBytes;

        var expectMessage = !_fileTransferToken.IsTransferComplete;
        if (expectMessage)
            _blobCursor?.MoveNext();
        else
            OnCompleted();
    }

    IEnumerator CreateBlobCursor()
    {
        ulong requestedPosition = 0;
        for (; requestedPosition + PartitionSize < bytesToSend; requestedPosition += PartitionSize)
        {
            RequestBlob(requestedPosition);
            yield return null;
        }
        RequestBlob(requestedPosition, (uint)(bytesToSend - requestedPosition));
    }

    void RequestBlob(ulong requestedPosition, uint size = PartitionSize)
    {
        ValueSet request = new();
        request.Add("ControlMessage", (uint)NearShareControlMsgType.FetchDataRequest);
        request.Add("BlobPosition", requestedPosition);
        request.Add("BlobSize", size);
        request.Add("ContentId", 0u);
        SendValueSet(request, _messageId);
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
