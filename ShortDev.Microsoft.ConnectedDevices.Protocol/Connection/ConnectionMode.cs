namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;

/// <summary>
/// Displays the types of available connections. <br/>
/// (See <see cref="ConnectionHeader.ConnectionMode"/>)
/// </summary>
public enum ConnectionMode : short
{
    None,
    Proximal,
    Legacy
}
