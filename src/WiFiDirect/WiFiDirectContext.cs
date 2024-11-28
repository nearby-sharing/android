using Android.Content;
using Android.Net.Wifi.P2p;
using System.Net;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using static Android.Net.Wifi.P2p.WifiP2pManager;

namespace NearShare.Droid.WiFiDirect;

internal sealed class WiFiDirectContext(Context context, WifiP2pManager manager, Channel channel) : Java.Lang.Object, IPeerListListener, IConnectionInfoListener
{
    public Context Context { get; } = context;
    public WifiP2pManager Manager { get; } = manager;
    public Channel Channel { get; } = channel;

    #region ConnectionInfoListener
    public event Action<WifiP2pInfo>? ConnectionInfoAvailable;
    public void OnConnectionInfoAvailable(WifiP2pInfo? info)
    {
        if (info is null)
            return;

        ConnectionInfoAvailable?.Invoke(info);
    }

    public async Task StartConnectAsync(WifiP2pConfig config)
    {
        ActionListener listener = new();
        Manager.Connect(Channel, config, listener);

        await listener;
    }

    public async Task<WifiP2pInfo> ConnectAsync(WifiP2pConfig config, CancellationToken cancellationToken)
    {
        cancellationToken.Register(() =>
        {
            Manager.CancelConnect(Channel, new ActionListener());
        });

        TaskCompletionSource<WifiP2pInfo> promise = new();
        ConnectionInfoAvailable += OnConnection;

        await StartConnectAsync(config);

        return await promise.Task
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        void OnConnection(WifiP2pInfo info)
        {
            ConnectionInfoAvailable -= OnConnection;
            promise.TrySetResult(info);
        }
    }
    #endregion

    #region PeerListener
    public IReadOnlyList<WifiP2pDevice> Peers { get; private set; } = [];
    public event Action<IReadOnlyList<WifiP2pDevice>>? PeersAvailable;
    void IPeerListListener.OnPeersAvailable(WifiP2pDeviceList? peers)
    {
        if (peers is null)
            return;

        var devices = Peers = peers.DeviceList?.ToArray() ?? [];
        PeersAvailable?.Invoke(devices);
    }

    public async Task StartDiscoveryAsync()
    {
        ActionListener listener = new();
        Manager.DiscoverPeers(Channel, listener);
        await listener;
    }

    public async Task StopDiscoveryAsync()
    {
        ActionListener listener = new();
        Manager.StopPeerDiscovery(Channel, listener);
        await listener;
    }

    public async Task<IReadOnlyList<WifiP2pDevice>> DiscoverPeersAsync()
    {
        TaskCompletionSource<IReadOnlyList<WifiP2pDevice>> peersPromise = new();
        PeersAvailable += OnPeers;

        await StartDiscoveryAsync();

        var result = await peersPromise.Task;

        await StopDiscoveryAsync();
        return result;

        void OnPeers(IReadOnlyList<WifiP2pDevice> devices)
        {
            PeersAvailable -= OnPeers;
            peersPromise.TrySetResult(devices);
        }
    }
    #endregion

    [SupportedOSPlatform("android29.0")]
    public async Task CreateGroupAsync(string ssid, string passphrase)
    {
        WifiP2pConfig config = new WifiP2pConfig.Builder()
            .SetNetworkName(ssid)
            .SetPassphrase(passphrase)
            .Build();

        ActionListener listener = new();
        Manager.CreateGroup(Channel, config, listener);
        await listener;
    }

    public static WiFiDirectContext Create(Context context)
    {
        var manager = (WifiP2pManager?)context.GetSystemService(Context.WifiP2pService) ?? throw new InvalidOperationException($"Could not get {nameof(WifiP2pManager)}");
        var channel = manager.Initialize(context, context.MainLooper, null) ?? throw new InvalidOperationException("Could not create WiFi-Direct channel");

        return new(context, manager, channel);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            Manager.RemoveGroup(Channel, new ActionListener());
            Manager.CancelConnect(Channel, new ActionListener());
            Manager.StopPeerDiscovery(Channel, new ActionListener());

            if (OperatingSystem.IsAndroidVersionAtLeast(27))
                Channel.Close();
        }
    }
}
