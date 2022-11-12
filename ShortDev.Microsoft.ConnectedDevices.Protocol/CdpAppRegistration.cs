using System;
using System.Collections.Generic;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

public static class CdpAppRegistration
{
    readonly record struct AppId(string Id, string Name, Func<ICdpApp> Factory);

    static Dictionary<string, AppId> _registration = new();

    public static bool TryRegisterApp<TApp>() where TApp : ICdpApp, ICdpAppId, new()
        => TryRegisterApp(TApp.Id, TApp.Name, () => new TApp());

    public static bool TryRegisterApp<TApp>(Func<TApp> factory) where TApp : ICdpApp, ICdpAppId
        => TryRegisterApp(TApp.Id, TApp.Name, () => factory());

    public static bool TryRegisterApp(string id, string name, Func<ICdpApp> factory)
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

    internal static ICdpApp InstantiateApp(string id, string name)
    {
        id = id.ToLower();
        lock (_registration)
            return _registration[id].Factory();
    }
}

public interface ICdpApp : IDisposable
{
    void HandleMessage(CdpChannel channel, CdpMessage msg);
}

public interface ICdpAppId
{
    static abstract string Id { get; }
    static abstract string Name { get; }
}
