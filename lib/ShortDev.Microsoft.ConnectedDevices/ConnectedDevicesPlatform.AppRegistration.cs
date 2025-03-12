using System.Collections.Concurrent;

namespace ShortDev.Microsoft.ConnectedDevices;

partial class ConnectedDevicesPlatform
{
    record AppId(string Id, string Name, CdpAppFactory<CdpAppBase> Factory);

    readonly ConcurrentDictionary<string, AppId> _registration = new();

    public void RegisterApp<TApp>(CdpAppFactory<TApp> factory) where TApp : CdpAppBase, ICdpAppId
        => RegisterApp(TApp.Id, TApp.Name, factory);

    public void RegisterApp(string id, string name, CdpAppFactory<CdpAppBase> factory)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(factory);

        id = id.ToLower();

        AppId appId = new(id, name, factory);
        _registration.AddOrUpdate(id, appId, (_, _) => appId);
    }

    public bool TryUnregisterApp<TApp>() where TApp : ICdpAppId
        => TryUnregisterApp(TApp.Id);

    public bool TryUnregisterApp(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        id = id.ToLower();
        return _registration.TryRemove(id, out _);
    }

    internal CdpAppBase InstantiateApp(string id, string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);

        id = id.ToLower();
        return _registration[id].Factory(this);
    }
}

/// <summary>
/// Signature of a <see cref="CdpAppBase"/> factory.
/// </summary>
public delegate T CdpAppFactory<out T>(ConnectedDevicesPlatform cdp) where T : CdpAppBase;
