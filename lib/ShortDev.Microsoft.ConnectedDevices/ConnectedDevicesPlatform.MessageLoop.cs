using ShortDev.IO.Buffers;
using ShortDev.IO.ValueStream;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices;

partial class ConnectedDevicesPlatform
{
    private void ReceiveLoop(CdpSocket socket)
    {
        RegisterKnownSocket(socket);
        Task.Run(() =>
        {
            var streamReader = EndianReader.FromStream(Endianness.BigEndian, socket.InputStream);
            using (socket)
            {
                ReceiveLoop(socket, ref streamReader);
            }
        });
    }

    void ReceiveLoop(CdpSocket socket, ref EndianReader<StreamWrapperStream> streamReader)
    {
        do
        {
            CdpSession? session = null;
            try
            {
                var header = CommonHeader.Parse(ref streamReader);

                if (socket.IsClosed)
                    return;

                session = CdpSession.GetOrCreate(
                this,
                    socket.Endpoint,
                    header
                );

                using var payload = MemoryPool.RentMemory(header.PayloadSize);
                streamReader.ReadBytes(payload.Span);

                if (socket.IsClosed)
                    return;

                var reader = EndianReader.FromMemory(Endianness.BigEndian, payload);
                session.HandleMessage(socket, header, ref reader);
            }
            catch (IOException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (socket.IsClosed)
                    return;

                if (session != null)
                    _logger.ExceptionInSession(ex, session.SessionId.AsNumber());
                else
                    _logger.ExceptionInReceiveLoop(ex, socket.TransportType);

                break;
            }
        } while (!socket.IsClosed);
    }
}
