using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using System;

namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// A cdp app is responsible for the application layer communication over an established <see cref="CdpChannel"/>. <br/>
/// Every channel has a unique app.
/// </summary>
public abstract class CdpAppBase(ConnectedDevicesPlatform cdp) : IDisposable
{
    CdpChannel? _channel;
    internal void Initialize(CdpChannel channel)
    {
        if (_channel != null)
            throw new InvalidOperationException("App already initialized");

        _channel = channel;
        OnInitialized(channel);
    }

    protected virtual void OnInitialized(CdpChannel channel) { }

    /// <summary>
    /// Gets the corresponding channel. <br/>
    /// The value is set immediately after instantiation. <br/>
    /// <br/>
    /// <inheritdoc cref="CdpChannel"/>
    /// </summary>
    public CdpChannel Channel => _channel ?? throw new InvalidOperationException("App is not initialized");

    /// <summary>
    /// Handle the received message.
    /// </summary>
    /// <param name="msg">Received message</param>
    public abstract void HandleMessage(CdpMessage msg);

    protected void SendValueSet(ValueSet request, uint msgId)
        => Channel.SendBinaryMessage(request.Write, msgId);

    public virtual void Dispose()
    {
        Channel.Dispose(closeSession: true, closeSocket: true);

        GC.SuppressFinalize(this);
    }
}
