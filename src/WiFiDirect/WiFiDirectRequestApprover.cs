using Android.Net.Wifi.P2p;
using Android.Net;
using Microsoft.Extensions.Logging;
using static Android.Net.Wifi.P2p.WifiP2pManager;
using System.Runtime.Versioning;

namespace NearShare.Droid.WiFiDirect;

[SupportedOSPlatform("android33.0")]
sealed class WiFiDirectRequestApprover(ILogger<WiFiDirectRequestApprover> logger, WiFiDirectContext context) : Java.Lang.Object, IExternalApproverRequestListener
{
    readonly ILogger<WiFiDirectRequestApprover> _logger = logger;
    readonly WiFiDirectContext _context = context;

    readonly List<MacAddress> _addresses = [];
    public void OnAttached(MacAddress deviceAddress)
        => _addresses.Add(deviceAddress);

    public void OnDetached(MacAddress deviceAddress, int reason)
        => _addresses.Remove(deviceAddress);

    public void OnConnectionRequested(int requestType, WifiP2pConfig config, WifiP2pDevice device)
    {
        if (device.DeviceAddress is null)
            return;

        var address = MacAddress.FromString(device.DeviceAddress);
        if (!_addresses.Contains(address))
            return;

        ConnectionRequestType result = ConnectionRequestType.Reject;
        switch ((ExternalApproverRequestType)requestType)
        {
            case ExternalApproverRequestType.Negotiation:
            case ExternalApproverRequestType.Invitation:
                result = ConnectionRequestType.Reject;
                break;

            case ExternalApproverRequestType.Join:
                result = ConnectionRequestType.Accept;
                break;
        }

        _logger.WiFiDirectApproveResult(result, (ExternalApproverRequestType)requestType, address);

        ActionListener promise = new();
        _context.Manager.SetConnectionRequestResult(_context.Channel, address, (int)result, promise);
    }

    public void OnPinGenerated(MacAddress deviceAddress, string pin) { }
}
