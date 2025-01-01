using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ShortDev.Microsoft.ConnectedDevices;

partial class ConnectedDevicesPlatform
{
    readonly ConcurrentDictionary<EndpointInfo, CdpSocket> _knownSockets = new();

    void RegisterKnownSocket(CdpSocket socket)
    {
        Debug.Assert(!socket.IsClosed);

        socket.Disposed += OnSocketClosed;
        void OnSocketClosed()
        {
            socket.Disposed -= OnSocketClosed;

            var couldRemove = _knownSockets.TryRemove(KeyValuePair.Create(socket.Endpoint, socket));
            Debug.Assert(couldRemove);
        }

        _knownSockets.AddOrUpdate(
            socket.Endpoint,
            static (key, newSocket) => newSocket,
            static (key, newSocket, currentSocket) => newSocket,
            socket
        );
    }

    bool TryGetKnownSocket(EndpointInfo endpoint, [MaybeNullWhen(false)] out CdpSocket socket)
    {
        if (!_knownSockets.TryGetValue(endpoint, out socket))
            return false;

        // ToDo: Alive check!!
        if (socket.IsClosed)
            return false;

        return true;
    }
}
