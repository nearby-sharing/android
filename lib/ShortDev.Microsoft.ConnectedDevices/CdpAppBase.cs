using ShortDev.Microsoft.ConnectedDevices.Messages;

namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// A cdp app is responsible for the application layer communication over an established <see cref="CdpChannel"/>. <br/>
/// Every channel has a unique app.
/// </summary>
public abstract class CdpAppBase : IDisposable
{
    /// <summary>
    /// Gets the corresponding channel. <br/>
    /// The value is set immediately after instantiation. <br/>
    /// <br/>
    /// <inheritdoc cref="CdpChannel"/>
    /// </summary>
    public CdpChannel Channel { get; }

    public CdpAppBase(CdpChannel channel)
    {
        channel.MessageReceived += OnMessageReceived;
        Channel = channel;
    }

    private void OnMessageReceived(object? sender, CdpMessage message)
        => HandleMessage(message);

    /// <summary>
    /// Handle the received message.
    /// </summary>
    /// <param name="msg">Received message</param>
    public abstract void HandleMessage(CdpMessage msg);

    public virtual void Dispose()
    {
        Channel.MessageReceived -= OnMessageReceived;
        Channel.Dispose(closeSession: true, closeSocket: true);

        GC.SuppressFinalize(this);
    }
}
