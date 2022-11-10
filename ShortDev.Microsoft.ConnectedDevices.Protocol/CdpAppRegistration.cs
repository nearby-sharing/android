using System;
using System.Collections.Generic;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

public static class CdpAppRegistration
{
    readonly record struct AppId(string Id, string Name, Func<ICdpApp> Factory);

    static Dictionary<string, AppId> _registration = new();

    public static void RegisterApp<TApp>() where TApp : ICdpApp, ICdpAppId, new()
        => RegisterApp(TApp.Id, TApp.Name, () => new TApp());

    public static void RegisterApp<TApp>(Func<TApp> factory) where TApp : ICdpApp, ICdpAppId
        => RegisterApp(TApp.Id, TApp.Name, () => factory());

    public static void RegisterApp(string id, string name, Func<ICdpApp> factory)
    {
        id = id.ToLower();
        lock (_registration)
        {
            if (_registration.ContainsKey(id))
                throw new ArgumentException($"Id {id} already exists!", nameof(id));

            _registration.Add(id, new(id, name, factory));
        }
    }

    public static void UnregisterApp<TApp>() where TApp : ICdpAppId
        => UnregisterApp(TApp.Id, TApp.Name);

    public static void UnregisterApp(string id, string name)
    {
        id = id.ToLower();
        lock (_registration)
        {
            _registration.Remove(id);
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
