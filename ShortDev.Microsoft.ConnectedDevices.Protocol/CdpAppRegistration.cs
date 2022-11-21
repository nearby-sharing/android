using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

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

    readonly record struct AppId(string Id, string Name, CdpAppFactory<CdpAppBase> Factory);

    static readonly Dictionary<string, AppId> _registration = new();

    public static bool TryRegisterApp<TApp>() where TApp : CdpAppBase, ICdpAppId, new()
        => TryRegisterApp(TApp.Id, TApp.Name, () => new TApp());

    public static bool TryRegisterApp<TApp>(CdpAppFactory<TApp> factory) where TApp : CdpAppBase, ICdpAppId
        => TryRegisterApp(TApp.Id, TApp.Name, factory);

    public static bool TryRegisterApp(string id, string name, CdpAppFactory<CdpAppBase> factory)
    {
        id = id.ToLower();
        lock (_registration)
        {
            return _registration.TryAdd(id, new(id, name, factory));
        }
    }

    public static bool TryUnregisterApp<TApp>() where TApp : ICdpAppId
        => TryUnregisterApp(TApp.Id);

    public static bool TryUnregisterApp(string id)
    {
        id = id.ToLower();
        lock (_registration)
        {
            return _registration.Remove(id);
        }
    }

    internal static CdpAppBase InstantiateApp(string id, string name)
    {
        id = id.ToLower();
        lock (_registration)
            return _registration[id].Factory();
    }
}

/// <summary>
/// A cdp app is responsible for the application layer communication over an established <see cref="CdpChannel"/>. <br/>
/// Every channel has a unique app.
/// </summary>
public abstract class CdpAppBase : IDisposable
{
    [AllowNull]
    public CdpChannel Channel { get; internal set; }

    public abstract ValueTask HandleMessageAsync(CdpMessage msg);

    /// <summary>
    /// Releases resources potentially used by the app itself.
    /// </summary>
    public virtual void Dispose() { }
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
