using Android.Content;
using Android.Net;
using Android.Net.Wifi.P2p;
using Android.Runtime;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using static Android.Net.Wifi.P2p.WifiP2pManager;

namespace NearShare.Droid;
internal sealed class AndroidWiFiDirectHandler : IWiFiDirectHandler
{
    readonly Receiver _receiver;
    public AndroidWiFiDirectHandler(Context context)
    {
        var manager = (WifiP2pManager?)context.GetSystemService(Context.WifiP2pService) ?? throw new InvalidOperationException($"Could not get {nameof(WifiP2pManager)}");
        var channel = manager.Initialize(context, context.MainLooper, null) ?? throw new InvalidOperationException("Could not create WiFi-Direct channel");

        _receiver = new(manager, channel);
        _receiver.Register(context);
    }

    sealed class Receiver(WifiP2pManager manager, Channel channel) : BroadcastReceiver, IPeerListListener, IConnectionInfoListener
    {
        public WifiP2pManager Manager { get; } = manager;
        public Channel Channel { get; } = channel;

        public WifiP2pState State { get; private set; } = WifiP2pState.Disabled;
        public WifiP2pDevice? CurrentDevice { get; private set; }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent == null)
                return;

            switch (intent.Action)
            {
                case WifiP2pStateChangedAction:
                    State = (WifiP2pState)intent.GetIntExtra(ExtraWifiState, -1);
                    break;

                case WifiP2pThisDeviceChangedAction:
                    CurrentDevice = intent.GetParcelableExtra<WifiP2pDevice>(ExtraWifiP2pDevice);
                    break;

                case WifiP2pPeersChangedAction:
                    Manager.RequestPeers(Channel, this);
                    break;

                case WifiP2pConnectionChangedAction:
                    var networkInfo = intent.GetParcelableExtra<NetworkInfo>(ExtraNetworkInfo);
                    if (networkInfo?.IsConnected != true)
                        break;

                    Manager.RequestConnectionInfo(Channel, this);
                    break;
            }
        }

        #region PeerListener
        public event Action<WifiP2pDevice[]>? PeersAvailable;
        public void OnPeersAvailable(WifiP2pDeviceList? peers)
        {
            if (peers is null)
                return;

            var devices = peers.DeviceList?.ToArray() ?? [];
            PeersAvailable?.Invoke(devices);
        }
        #endregion

        #region ConnectionInfoListener
        public event Action<WifiP2pInfo>? ConnectionInfoAvailable;
        public void OnConnectionInfoAvailable(WifiP2pInfo? info)
        {
            if (info is null)
                return;

            ConnectionInfoAvailable?.Invoke(info);
        }
        #endregion

        public void Register(Context context)
        {
            IntentFilter intentFilter = new();
            intentFilter.AddAction(WifiP2pPeersChangedAction);
            intentFilter.AddAction(WifiP2pConnectionChangedAction);
            intentFilter.AddAction(WifiP2pThisDeviceChangedAction);

            context.RegisterReceiver(this, intentFilter);
        }

        public void Unregister(Context context)
        {
            context.UnregisterReceiver(this);
        }
    }

    public bool IsEnabled => _receiver?.State == WifiP2pState.Enabled;

    public PhysicalAddress MacAddress => throw new NotImplementedException();

    public async Task<IPAddress> ConnectAsync(EndpointInfo endpoint, CancellationToken cancellationToken = default)
    {
        WifiP2pConfig config = new()
        {
            DeviceAddress = endpoint.Address
        };

        TaskCompletionSource<WifiP2pInfo> promise = new();
        _receiver.ConnectionInfoAvailable += OnConnection;

        await _receiver.Manager.StartConnectAsync(_receiver.Channel, config);

        var info = await promise.Task;
        return IPAddress.Parse(info.GroupOwnerAddress!.HostAddress!);

        void OnConnection(WifiP2pInfo info)
        {
            _receiver.ConnectionInfoAvailable -= OnConnection;
            promise.TrySetResult(info);
        }
    }

    public void Dispose()
    {
        // ToDo
    }
}

static class WiFiDirectExtensions
{
    public static async Task StartDiscoveryAsync(this WifiP2pManager manager, Channel channel)
    {
        ActionListener listener = new();
        manager.DiscoverPeers(channel, listener);
        await listener;
    }

    public static async Task StartConnectAsync(this WifiP2pManager manager, Channel channel, WifiP2pConfig config)
    {
        ActionListener listener = new();
        manager.Connect(channel, config, listener);
        await listener;
    }

    sealed class ActionListener : Java.Lang.Object, IActionListener
    {
        readonly TaskCompletionSource _promise = new();
        public void OnFailure([GeneratedEnum] WifiP2pFailureReason reason)
            => _promise.SetException(new InvalidOperationException(reason.ToString()));

        public void OnSuccess()
            => _promise.SetResult();

        public TaskAwaiter GetAwaiter()
            => _promise.Task.GetAwaiter();
    }
}
