using ShortDev.Microsoft.ConnectedDevices.Protocol.Serialization;
using ShortDev.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.NearShare;

public sealed class NearShareApp : CdpAppBase
{
    public static string Name { get; } = "NearSharePlatform";

    public required INearSharePlatformHandler PlatformHandler { get; init; }
    public required string Id { get; init; }

    const uint PartitionSize = 102400u; // 131072u

    ulong transferedBytes = 0;
    ulong bytesToSend = 0;
    FileTransferToken? _fileTransferToken;

    public override async ValueTask HandleMessageAsync(CdpMessage msg)
    {
        bool expectMessage = true;

        CommonHeader header = msg.Header;
        BinaryReader payloadReader = msg.Read();

        var prepend = payloadReader.ReadBytes(0x0000000C);
        var buffer = payloadReader.ReadPayload();
        Debug.Print(BinaryConvert.ToString(buffer));
        var payload = ValueSet.Parse(buffer);

        header.AdditionalHeaders.RemoveAll((x) => x.Type == AdditionalHeaderType.CorrelationVector);

        // if (header.HasFlag(MessageFlags.ShouldAck))
        //      Channel.SendAck(header);

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

                            PlatformHandler.Log(0, $"Receiving file \"{fileNames[0]}\" from session {header.SessionId.ToString("X")}");

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
                                SendCancel(header, prepend);
                                CloseChannel();
                                throw;
                            }

                            ulong requestedPosition = 0;
                            for (; requestedPosition + PartitionSize < bytesToSend; requestedPosition += PartitionSize)
                                RequestBlob(header, prepend, requestedPosition);
                            RequestBlob(header, prepend, requestedPosition, (uint)(bytesToSend - requestedPosition));

                            return;
                        }
                        else if (dataKind == DataKind.Uri)
                        {
                            var uri = payload.Get<string>("Uri");
                            PlatformHandler.Log(0, $"Received uri \"{uri}\" from session {header.SessionId.ToString("X")}");
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

        header.Flags = 0;
        Channel.SendMessage(header, (payloadWriter) =>
        {
            payloadWriter.Write(prepend);
            response.Write(payloadWriter);
        });

        if (!expectMessage)
            CloseChannel();
    }

    void RequestBlob(CommonHeader header, byte[] prepend, ulong requestedPosition, uint size = PartitionSize)
    {
        ValueSet request = new();
        request.Add("BlobPosition", requestedPosition);
        request.Add("BlobSize", size);
        request.Add("ContentId", 0u);
        request.Add("ControlMessage", (uint)NearShareControlMsgType.FetchDataRequest);

        header.Flags = 0;
        Channel.SendMessage(header, (payloadWriter) =>
        {
            payloadWriter.Write(prepend);
            request.Write(payloadWriter);
        });
    }

    void SendCancel(CommonHeader header, byte[] prepend)
    {
        ValueSet request = new();
        request.Add("ControlMessage", (uint)NearShareControlMsgType.CancelTransfer);

        header.Flags = 0;
        Channel.SendMessage(header, (payloadWriter) =>
        {
            payloadWriter.Write(prepend);
            request.Write(payloadWriter);
        });
    }

    void CloseChannel()
    {
        Channel.Dispose(closeSession: true, closeSocket: true);
        CdpAppRegistration.TryUnregisterApp(Id);
    }
}
