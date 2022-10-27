namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Session.AppControl;

public enum AppControlType : byte
{
    LaunchUri = 0,
    LaunchUriResult,
    LaunchUriForTarget,
    CallAppService,
    CallAppServiceResponse,
    GetResource,
    GetResourceResponse,
    SetResource,
    SetResourceResponse
}
