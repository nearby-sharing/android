using Android.OS;

namespace Nearby_Sharing_Windows.Service;

internal sealed class CdpServiceBinder : Binder
{
    public CdpServiceBinder(CdpService service)
       => Service = service;

    public CdpService Service { get; private set; }
}
