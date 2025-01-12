using Android.Content;
using Android.Net;
using Android.Net.Wifi.P2p;
using NearShare.Utils;
using static Android.Net.Wifi.P2p.WifiP2pManager;

namespace NearShare.Droid.WiFiDirect;

internal sealed class WiFiDirectCallbackReceiver : BroadcastReceiver
{
    readonly WiFiDirectContext _context;
    public WiFiDirectCallbackReceiver(WiFiDirectContext context)
    {
        _context = context;

        IntentFilter intentFilter = new();
        intentFilter.AddAction(WifiP2pStateChangedAction);
        intentFilter.AddAction(WifiP2pThisDeviceChangedAction);
        intentFilter.AddAction(WifiP2pPeersChangedAction);
        intentFilter.AddAction(WifiP2pConnectionChangedAction);

        context.Context.RegisterReceiver(this, intentFilter);
    }

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
                _context.Manager.RequestPeers(_context.Channel, _context);
                break;

            case WifiP2pConnectionChangedAction:
                var networkInfo = intent.GetParcelableExtra<NetworkInfo>(ExtraNetworkInfo);
                if (networkInfo?.IsConnected != true)
                    break;

                _context.Manager.RequestConnectionInfo(_context.Channel, _context);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _context.Context.UnregisterReceiver(this);
        }
    }
}
