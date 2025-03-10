﻿using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Diagnostics.CodeAnalysis;

namespace ShortDev.Microsoft.ConnectedDevices;

public sealed partial class ConnectedDevicesPlatform(LocalDeviceInfo deviceInfo, ILoggerFactory loggerFactory) : IDisposable
{
    public LocalDeviceInfo DeviceInfo { get; } = deviceInfo;

    readonly ILogger<ConnectedDevicesPlatform> _logger = loggerFactory.CreateLogger<ConnectedDevicesPlatform>();

    #region Listen
    public async ValueTask StartListen(CancellationToken cancellation = default)
    {
        _logger.ListeningStarted();
        try
        {
            await Task.WhenAll(_transportMap.Values
                .Select(async transport =>
                {
                    transport.DeviceConnected += OnDeviceConnected;
                    await transport.StartListen(cancellation).ConfigureAwait(false);
                })
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.ListeningError(ex);
        }
    }

    public async ValueTask StopListen(CancellationToken cancellation = default)
    {
        try
        {
            await Task.WhenAll(_transportMap.Values
                .Select(async transport =>
                {
                    await transport.StopListen(cancellation).ConfigureAwait(false);
                    transport.DeviceConnected -= OnDeviceConnected;
                })
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.ListeningError(ex);
        }
        finally
        {
            _logger.ListeningStopped();
        }
    }

    private void OnDeviceConnected(ICdpTransport sender, CdpSocket socket)
    {
        _logger.NewSocket(socket.Endpoint);
        ReceiveLoop(socket);
    }
    #endregion

    #region Client
    public async Task<CdpSession> ConnectAsync([NotNull] EndpointInfo endpoint, ConnectOptions? options = null, CancellationToken cancellationToken = default)
    {
        var socket = await CreateSocketAsync(endpoint, cancellationToken).ConfigureAwait(false);
        return await CdpSession.ConnectClientAsync(this, socket, options, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CdpSocket> CreateSocketAsync(EndpointInfo endpoint, CancellationToken cancellationToken = default)
    {
        if (TryGetKnownSocket(endpoint, out var knownSocket))
            return knownSocket;

        var transport = TryGetTransport(endpoint.TransportType) ?? throw new InvalidOperationException($"No transport found for type {endpoint.TransportType}");
        var socket = await transport.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
        ReceiveLoop(socket);
        return socket;
    }

    internal async Task<CdpSocket?> TryCreateSocketAsync(EndpointInfo endpoint, TimeSpan connectTimeout)
    {
        if (TryGetKnownSocket(endpoint, out var knownSocket))
            return knownSocket;

        var transport = TryGetTransport(endpoint.TransportType);
        if (transport == null)
            return null;

        var socket = await transport.TryConnectAsync(endpoint, connectTimeout).ConfigureAwait(false);
        if (socket == null)
            return null;

        ReceiveLoop(socket);
        return socket;
    }
    #endregion

    public CdpDeviceInfo GetCdpDeviceInfo()
    {
        List<EndpointInfo> endpoints = [];
        foreach (var (_, transport) in _transportMap)
        {
            try
            {
                var endpoint = transport.GetEndpoint();
                endpoints.Add(endpoint);
            }
            catch { }
        }
        return DeviceInfo.ToCdpDeviceInfo(endpoints);
    }

    public ILogger<T> CreateLogger<T>()
        => loggerFactory.CreateLogger<T>();

    public void Dispose()
    {
        Extensions.DisposeAll(
            _transportMap.Select(x => x.Value),
            _knownSockets.Select(x => x.Value)
        );

        _transportMap.Clear();
        _knownSockets.Clear();
    }
}
