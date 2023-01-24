using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices;

public sealed class ConnectedDevicesPlatform : IDisposable
{
    public ICdpPlatformHandler Handler { get; }

    public ConnectedDevicesPlatform(ICdpPlatformHandler platform)
    {
        Handler = platform;
    }

    #region Transport
    readonly ConcurrentDictionary<Type, ICdpTransport> _transports = new();
    public void AddTransport<T>(T transport) where T : ICdpTransport
    {
        _transports.AddOrUpdate(typeof(T), transport, (_, old) =>
        {
            old.Dispose();
            return transport;
        });
    }

    public T? TryGetTransport<T>() where T : ICdpTransport
        => (T?)_transports.GetValueOrDefault(typeof(T));
    #endregion

    #region Host
    #region Advertise
    public bool IsAdvertising { get; private set; } = false;
    public void Advertise(CdpAdvertisement options, CancellationToken cancellationToken)
    {
        lock (this)
        {
            if (IsAdvertising)
                return;

            IsAdvertising = true;
        }

        foreach (var (_, transport) in _transports)
            if (transport is ICdpDiscoverableTransport discoverableTransport)
                discoverableTransport.Advertise(options, cancellationToken);

        cancellationToken.Register(() =>
        {
            lock (this)
                IsAdvertising = false;
        });
    }
    #endregion

    #region Listen
    public bool IsListening { get; private set; } = false;
    public void Listen(CancellationToken cancellationToken)
    {
        lock (this)
        {
            if (IsListening)
                return;

            IsListening = true;
        }

        foreach (var (_, transport) in _transports)
        {
            transport.Listen(cancellationToken);
            transport.DeviceConnected += OnDeviceConnected;
        }

        cancellationToken.Register(() =>
        {
            lock (this)
                IsListening = false;
        });
    }

    private void OnDeviceConnected(ICdpTransport sender, CdpSocket socket)
    {
        Handler.Log(0, $"Device {socket.RemoteDevice.Name} ({socket.RemoteDevice.Address}) connected via {socket.TransportType}");
        ReceiveLoop(socket);
    }
    #endregion
    #endregion

    #region Client
    public void Discover(CancellationToken cancellationToken)
    {
        foreach (var (_, transport) in _transports)
        {
            if(transport is ICdpDiscoverableTransport discoverableTransport)
            {
                discoverableTransport.Discover(cancellationToken);
                discoverableTransport.DeviceDiscovered += DeviceDiscovered;
            }
        }
    }

    public event DeviceDiscoveredEventHandler? DeviceDiscovered;

    public async Task<CdpSession> ConnectAsync(CdpDevice device)
    {
        var transport = TryGetTransport<BluetoothTransport>() ?? throw new InvalidOperationException("Bluetooth transport is needed!");
        var socket = await transport.ConnectAsync(device);
        ReceiveLoop(socket);
        return CdpSession.CreateAndConnectClient(this, socket);
    }
    #endregion

    private void ReceiveLoop(CdpSocket socket)
    {
        Task.Run(() =>
        {
            var reader = socket.Reader;
            using (socket)
            {
                do
                {
                    CdpSession? session = null;
                    try
                    {
                        var header = CommonHeader.Parse(reader);
                        session = CdpSession.GetOrCreate(
                            this,
                            socket.RemoteDevice ?? throw new InvalidDataException(),
                            header
                        );
                        session.HandleMessage(socket, header, reader);
                    }
                    catch (Exception ex)
                    {
                        Handler.Log(1, $"{ex.GetType().Name} in session {session?.LocalSessionId.ToString() ?? "null"} \n {ex.Message}");
                        break;
                    }
                } while (!socket.IsClosed);
            }
        });
    }

    public void Dispose()
    {
        foreach (var (_, transport) in _transports)
            transport.Dispose();
    }
}
