using System;
using System.Collections.Concurrent;

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
    public delegate T CdpAppFactory<out T>(ConnectedDevicesPlatform cdp) where T : CdpAppBase;

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

    internal static CdpAppBase InstantiateApp(string id, string name, ConnectedDevicesPlatform cdp)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);

        id = id.ToLower();
        return _registration[id].Factory(cdp);
    }
}
