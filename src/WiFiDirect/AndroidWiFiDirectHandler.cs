using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.Net.Wifi.P2p;
using Android.Runtime;
using ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using static Android.Net.Wifi.P2p.WifiP2pManager;

namespace NearShare.Droid.WiFiDirect;
internal sealed class AndroidWiFiDirectHandler : IWiFiDirectHandler
{
    readonly WiFiDirectContext _context;
    readonly WiFiDirectCallbackReceiver _receiver;
    public AndroidWiFiDirectHandler(Context context)
    {
        _context = WiFiDirectContext.Create(context);
        _receiver = new(_context);
    }

    public bool IsEnabled => _receiver.State == WifiP2pState.Enabled;

    public PhysicalAddress MacAddress { get; } = PhysicalAddress.Parse("8c:b8:4a:5d:47:50");

    public async Task<IPAddress> ConnectAsync(string address, string ssid, ReadOnlyMemory<byte> sharedKey, CancellationToken cancellationToken = default)
    {
        var wifiManager = (WifiManager)_context.Context.GetSystemService(Context.WifiService)!;

        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            WifiConfiguration wifiConfiguration = new()
            {
                Ssid = $"\"{ssid}\"",
                PreSharedKey = Convert.ToHexString(sharedKey.Span),
            };

            wifiManager.AddNetwork(wifiConfiguration);
            wifiManager.EnableNetwork(wifiConfiguration.NetworkId, true);
            wifiManager.Reconnect();

            // ToDo: Get GO IP address
            return null;
        }

        // ToDo: Show info of disconnection
        // wifiManager.IsStaConcurrencyForLocalOnlyConnectionsSupported

        var connectivity = (ConnectivityManager)_context.Context.GetSystemService(Context.ConnectivityService)!;

        var specifier = new WifiNetworkSpecifier.Builder()
            .SetSsid(ssid)
            .SetWpa2Passphrase("ThisIsNotTheActualPassphrase")
            .Build();

        var abc = wifiManager.ScanResults?.Where(x => x.Ssid?.StartsWith("DIRECT-") == true).ToArray() ?? [];
        System.Diagnostics.Debug.Print(abc.ToString());

        var config = (WifiConfiguration)specifier.Class
            .GetDeclaredField("wifiConfiguration")
            .Get(specifier)!;
#pragma warning disable CA1422 // Validate platform compatibility
        // config.PreSharedKey = Convert.ToHexString(sharedKey.Span);
#pragma warning restore CA1422 // Validate platform compatibility

        var request = new NetworkRequest.Builder()
            .AddTransportType(TransportType.Wifi)!
            .SetNetworkSpecifier(specifier)!
            .RemoveCapability(NetCapability.Internet)!
            .Build()!;

        ConnectRequestCallback result = new(connectivity);
        connectivity.RequestNetwork(request, result);
        try
        {
            return await result;
        }
        finally
        {
            connectivity.UnregisterNetworkCallback(result);
        }
    }

    [SupportedOSPlatform("android29.0")]
    sealed class ConnectRequestCallback(ConnectivityManager connectivityManager) : ConnectivityManager.NetworkCallback
    {
        readonly TaskCompletionSource<IPAddress> _promise = new();
        readonly ConnectivityManager _connectivityManager = connectivityManager;
        public override void OnAvailable(Network network)
        {
            _connectivityManager.BindProcessToNetwork(network);

            // ToDo: Get GO IP address
            var linkProps = _connectivityManager.GetLinkProperties(network);

        }

        public override void OnBlockedStatusChanged(Network network, bool blocked)
        {
            base.OnBlockedStatusChanged(network, blocked);
        }

        public override void OnCapabilitiesChanged(Network network, NetworkCapabilities networkCapabilities)
        {
            base.OnCapabilitiesChanged(network, networkCapabilities);
        }

        public override void OnLinkPropertiesChanged(Network network, LinkProperties linkProperties)
        {
            base.OnLinkPropertiesChanged(network, linkProperties);
        }

        public override void OnLosing(Network network, int maxMsToLive)
        {
            base.OnLosing(network, maxMsToLive);
        }

        public override void OnLost(Network network)
        {
            base.OnLost(network);
        }

        public override void OnUnavailable()
            => _promise.TrySetException(new InvalidOperationException("Network is unavailable"));

        public TaskAwaiter<IPAddress> GetAwaiter()
            => _promise.Task.GetAwaiter();
    }

    public async Task<IPAddress> ConnectAsync2(string address, string ssid, ReadOnlyMemory<byte> sharedKey, CancellationToken cancellationToken = default)
    {
        var peers = await _context.DiscoverPeersAsync();

        var peer = peers.FirstOrDefault(x => string.Equals(x.DeviceAddress, address, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Peer '{address}' is unknown");

        WifiP2pConfig config;
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            config = new WifiP2pConfig.Builder()
                .EnablePersistentMode(persistent: true)
                .SetDeviceAddress(Android.Net.MacAddress.FromString(peer.DeviceAddress!))
                .SetNetworkName(ssid)
                .SetPassphrase(Convert.ToHexString(sharedKey.Span))
                .Build();
        }
        else
        {
            config = new()
            {
                DeviceAddress = peer.DeviceAddress
            };
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(34))
            ForceJoinGroup(config);

        var info = await _context.ConnectAsync(config, cancellationToken);

        return IPAddress.Parse(info.GroupOwnerAddress!.HostAddress!);
    }

    public async Task CreateGroupAutonomous(string ssid, ReadOnlyMemory<byte> passphrase)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            throw new InvalidOperationException("Not supported on OS < 29");

        await _context.CreateGroupAsync(ssid, passphrase);
    }

    [SupportedOSPlatform("android34.0")]
    static void ForceJoinGroup(WifiP2pConfig config)
    {
        try
        {
            var field = config.Class.GetDeclaredField("mJoinExistingGroup");
            field.Accessible = true;
            field.SetBoolean(config, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Print(ex.GetType().Name + Environment.NewLine + ex.Message);
        }

        System.Diagnostics.Debug.Print(config.ToString());
    }

    public void Dispose()
    {
        _receiver.Dispose();
        _context.Dispose();
    }
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
