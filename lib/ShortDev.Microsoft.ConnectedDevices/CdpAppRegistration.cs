using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

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
    public delegate T CdpAppFactory<out T>(CdpChannel channel) where T : CdpAppBase;

    private readonly record struct AppId(string Id, string Name, CdpAppFactory<CdpAppBase> Factory);

    private static readonly ConcurrentDictionary<string, AppId> _registration = new(StringComparer.OrdinalIgnoreCase);

    public static void RegisterApp<TApp>() where TApp : CdpAppBase, ICdpAppFactory<TApp>, ICdpAppId
        => RegisterApp(TApp.Id, TApp.Name, TApp.Create);

    public static void RegisterApp<TApp>(CdpAppFactory<TApp> factory) where TApp : CdpAppBase, ICdpAppId
        => RegisterApp(TApp.Id, TApp.Name, factory);

    public static void RegisterApp(string id, string name, CdpAppFactory<CdpAppBase> factory)
    {
        AppId appId = new(id, name, factory);
        _registration.AddOrUpdate(id, appId, (_, _) => appId);
    }

    public static bool TryUnregisterApp<TApp>() where TApp : ICdpAppId
        => TryUnregisterApp(TApp.Id);

    public static bool TryUnregisterApp(string id)
        => _registration.TryRemove(id, out _);

    internal static CdpAppBase InstantiateApp(string id, string name, CdpChannel channel)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _registration[id].Factory(channel);
    }

    internal static bool TryGetAppFactory(string id, string name, [MaybeNullWhen(false)] out CdpAppFactory<CdpAppBase> factory)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (!_registration.TryGetValue(id, out var appId))
        {
            factory = null;
            return false;
        }

        factory = appId.Factory;
        return true;
    }
}

public interface ICdpAppFactory<out TApp>
    where TApp : CdpAppBase
{
    static abstract TApp Create(CdpChannel channel);
}
