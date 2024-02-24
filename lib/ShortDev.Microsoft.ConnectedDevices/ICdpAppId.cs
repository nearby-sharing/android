namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// Describes a static cdp app with well known id. <br/>
/// Implementing apps can be instantiated without prior handshake.
/// </summary>
public interface ICdpAppId
{
    static abstract string Id { get; }
    static abstract string Name { get; }
}
