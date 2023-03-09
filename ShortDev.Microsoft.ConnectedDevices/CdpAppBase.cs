using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// A cdp app is responsible for the application layer communication over an established <see cref="CdpChannel"/>. <br/>
/// Every channel has a unique app.
/// </summary>
public abstract class CdpAppBase
{
    /// <summary>
    /// Gets the corresponding channel. <br/>
    /// The value is set immediately after instantiation. <br/>
    /// <br/>
    /// <inheritdoc cref="CdpChannel"/>
    /// </summary>
    [AllowNull]
    public CdpChannel Channel { get; internal set; }

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
