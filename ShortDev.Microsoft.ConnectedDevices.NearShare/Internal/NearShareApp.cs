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

    ulong transferedBytes = 0;
    ulong bytesToSend = 0;
    FileTransferToken? _fileTransferToken;

    IEnumerator? _blobCursor;

    public override async ValueTask HandleMessageAsync(CdpMessage msg)
    {
        bool expectMessage = true;

        var payload = ValueSet.Parse(msg.Read());

        ValueSet response = new();
        if (payload.ContainsKey("ControlMessage"))
        {
            var msgType = (NearShareControlMsgType)payload.Get<uint>("ControlMessage");
            switch (msgType)
            {
                case NearShareControlMsgType.StartTransfer:
                    {
                        var dataKind = (DataKind)payload.Get<uint>("DataKind");
                        if (dataKind == DataKind.File)
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
                            PlatformHandler.OnFileTransfer(_fileTransferToken);

                            try
                            {
                                await _fileTransferToken.WaitForAcceptance();
                            }
                            catch (TaskCanceledException)
                            {
                                SendCancel();
                                CloseChannel();
                                throw;
                            }

                            _blobCursor = CreateBlobCursor();
                            _blobCursor.MoveNext();
                            return;
                        }
                        else if (dataKind == DataKind.Uri)
                        {
                            var uri = payload.Get<string>("Uri");
                            PlatformHandler.Log(0, $"Received uri \"{uri}\" from session {msg.Header.SessionId:X}");
                            PlatformHandler.OnReceivedUri(new()
                            {
                                DeviceName = Channel.Session.Device.Name ?? "UNKNOWN",
                                Uri = uri
                            });
                            expectMessage = false;
                        }
                        else
                            throw new NotImplementedException($"DataKind {dataKind} not implemented");
                        break;
                    }
                case NearShareControlMsgType.FetchDataResponse:
                    {
                        if (_fileTransferToken == null)
                            throw new InvalidOperationException();

                        var position = payload.Get<ulong>("BlobPosition");
                        var blob = payload.Get<List<byte>>("DataBlob");
                        var blobSize = (ulong)blob.Count;

                        var newPosition = position + blobSize;
                        if (position > bytesToSend || blobSize > PartitionSize)
                            throw new InvalidOperationException("Device tried to send too much data!");

                        // PlatformHandler.Log(0, $"BlobPosition: {position}; ({newPosition * 100 / bytesToSend}%)");
                        lock (_fileTransferToken)
                        {
                            var stream = _fileTransferToken.Stream;
                            stream.Position = (long)position;
                            stream.Write(CollectionsMarshal.AsSpan(blob));
                        }

                        transferedBytes += blobSize;
                        _fileTransferToken.ReceivedBytes = transferedBytes;

                        expectMessage = !_fileTransferToken.IsTransferComplete;

                        if (expectMessage)
                        {
                            _blobCursor?.MoveNext();
                            return;
                        }
                        break;
                    }
            }
        }
        else
            expectMessage = false;

        if (!expectMessage)
        {
            // Finished
            response.Add("ControlMessage", (uint)NearShareControlMsgType.CompleteTransfer);
        }

        Channel.SendMessage(response.Write);

        if (!expectMessage)
            CloseChannel();
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
        request.Add("BlobPosition", requestedPosition);
        request.Add("BlobSize", size);
        request.Add("ContentId", 0u);
        request.Add("ControlMessage", (uint)NearShareControlMsgType.FetchDataRequest);

        Channel.SendMessage(request.Write);
    }

    void SendCancel()
    {
        ValueSet request = new();
        request.Add("ControlMessage", (uint)NearShareControlMsgType.CancelTransfer);

        Channel.SendMessage(request.Write);
    }

    void CloseChannel()
    {
        Channel.Dispose(closeSession: true, closeSocket: true);
        CdpAppRegistration.TryUnregisterApp(Id);
    }
}
