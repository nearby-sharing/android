using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

/// <summary>
/// Handles message received by a <see cref="CdpChannel"/>.
/// </summary>
public interface IChannelMessageHandler
{
    /// <summary>
    /// Handle the received message.
    /// </summary>
    /// <param name="msg">Received message</param>
    void HandleMessage(CdpMessage msg);
}
