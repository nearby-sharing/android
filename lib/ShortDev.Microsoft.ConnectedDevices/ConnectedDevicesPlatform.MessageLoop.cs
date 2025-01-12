using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Buffers;

namespace ShortDev.Microsoft.ConnectedDevices;

partial class ConnectedDevicesPlatform
{
    static readonly ArrayPool<byte> _messagePool = ArrayPool<byte>.Create();
    private void ReceiveLoop(CdpSocket socket)
    {
        RegisterKnownSocket(socket);
        Task.Run(() =>
        {
            EndianReader streamReader = new(Endianness.BigEndian, socket.InputStream);
            using (socket)
            {
                ReceiveLoop(socket, ref streamReader);
            }
        });
    }

    void ReceiveLoop(CdpSocket socket, ref EndianReader streamReader)
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

                using var payload = _messagePool.RentToken(header.PayloadSize);
                streamReader.ReadBytes(payload.Span);

                if (socket.IsClosed)
                    return;

                EndianReader reader = new(Endianness.BigEndian, payload.Span);
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
