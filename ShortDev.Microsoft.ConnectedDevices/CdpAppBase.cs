using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// A cdp app is responsible for the application layer communication over an established <see cref="CdpChannel"/>. <br/>
/// Every channel has a unique app.
/// </summary>
public abstract class CdpAppBase
{
    [AllowNull] CdpChannel _channel;
    /// <summary>
    /// Gets the corresponding channel. <br/>
    /// The value is set immediately after instantiation. <br/>
    /// <br/>
    /// <inheritdoc cref="CdpChannel"/>
    /// </summary>
    public CdpChannel Channel
    {
        get => _channel;
        internal set
        {
            _channel = value;
            OnInitialized(value);
        }
    }

    protected virtual void OnInitialized(CdpChannel channel) { }

    /// <summary>
    /// Handle the received message.
    /// </summary>
    /// <param name="msg">Received message</param>
    public abstract void HandleMessage(CdpMessage msg);

    protected void SendValueSet(ValueSet request, uint msgId)
        => Channel.SendBinaryMessage(request.Write, msgId);

    protected virtual void CloseChannel()
    {
        Channel.Dispose(closeSession: true, closeSocket: true);
    }
}
