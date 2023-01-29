using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// Provides a global registry for static apps. <br/>
/// (See <see cref="CdpAppBase"/>)
/// </summary>
public static class CdpAppRegistration
{
    /// <summary>
    /// Signature of a <see cref="CdpAppBase"/> factory.
    /// </summary>
    public delegate T CdpAppFactory<out T>() where T : CdpAppBase;

    record AppId(string Id, string Name, CdpAppFactory<CdpAppBase> Factory);

    static readonly ConcurrentDictionary<string, AppId> _registration = new();

    public static void RegisterApp<TApp>(CdpAppFactory<TApp> factory) where TApp : CdpAppBase, ICdpAppId
        => RegisterApp(TApp.Id, TApp.Name, factory);

    public static void RegisterApp(string id, string name, CdpAppFactory<CdpAppBase> factory)
    {
        id = id.ToLower();

        AppId appId = new(id, name, factory);
        _registration.AddOrUpdate(id, appId, (_, _) => appId);
    }

    public static bool TryUnregisterApp<TApp>() where TApp : ICdpAppId
        => TryUnregisterApp(TApp.Id);

    public static bool TryUnregisterApp(string id)
    {
        id = id.ToLower();
        return _registration.TryRemove(id, out _);
    }

    internal static CdpAppBase InstantiateApp(string id, string name)
    {
        id = id.ToLower();
        return _registration[id].Factory();
    }
}

/// <summary>
/// A cdp app is responsible for the application layer communication over an established <see cref="CdpChannel"/>. <br/>
/// Every channel has a unique app.
/// </summary>
public abstract class CdpAppBase : IChannelMessageHandler
{
    /// <summary>
    /// Gets the corresponding channel. <br/>
    /// The value is set immediately after instantiation. <br/>
    /// <br/>
    /// <inheritdoc cref="CdpChannel"/>
    /// </summary>
    [AllowNull]
    public CdpChannel Channel { get; internal set; }

    public abstract ValueTask HandleMessageAsync(CdpMessage msg);
}

/// <summary>
/// Describes a static cdp app with well known id. <br/>
/// Implementing apps can be instantiated without prior handshake.
/// </summary>
public interface ICdpAppId
{
    static abstract string Id { get; }
    static abstract string Name { get; }
}
