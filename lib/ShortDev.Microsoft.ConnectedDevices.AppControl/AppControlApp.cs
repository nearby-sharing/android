using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.AppControl.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages;

namespace ShortDev.Microsoft.ConnectedDevices.AppControl;

public abstract class AppControlApp(CdpChannel channel) : CdpAppBase(channel)
{
    readonly ILogger<AppControlApp> _logger = channel.Session.Platform.CreateLogger<AppControlApp>();

    public sealed override void HandleMessage(CdpMessage msg)
    {
        msg.Read(out var reader);

        var header = AppControlHeader.Parse(ref reader);
        switch (header.MessageType)
        {
            // == Uri == //

            case AppControlType.LaunchUri:
                OnLaunchUri(LaunchUriRequest.Parse(ref reader));
                break;

            case AppControlType.LaunchUriForTarget:
                OnLaunchUriForTarget(LaunchUriForTargetRequest.Parse(ref reader));
                break;

            case AppControlType.LaunchUriResult:
                OnLaunchUriResult(LaunchUriResult.Parse(ref reader));
                break;

            // == AppService == //

            case AppControlType.CallAppService:
                OnCallAppService(CallAppServiceRequest.Parse(ref reader));
                break;

            case AppControlType.CallAppServiceResponse:
                OnCallAppServiceResponse(CallAppServiceResponse.Parse(ref reader));
                break;

            // == Resources == //

            case AppControlType.GetResource:
                OnGetResource(GetResourceRequest.Parse(ref reader));
                break;

            case AppControlType.GetResourceResponse:
                OnGetResourceResponse(GetResourceResponse.Parse(ref reader));
                break;

            case AppControlType.SetResource:
                OnSetResource(SetResourceRequest.Parse(ref reader));
                break;

            case AppControlType.SetResourceResponse:
                OnSetResourceResponse(SetResourceResponse.Parse(ref reader));
                break;

            default:
                _logger.UnexpectedMessage(header.MessageType);
                break;
        }
    }

    const uint E_NOTIMPL = 0x80004001;

    #region Launch Uri
    protected virtual void OnLaunchUri(in LaunchUriRequest request)
        => SendAppControlMessage(AppControlType.LaunchUriResult, new LaunchUriResult()
        {
            ResponseID = request.RequestID,
            Result = E_NOTIMPL
        });

    protected virtual void OnLaunchUriForTarget(in LaunchUriForTargetRequest request)
        => SendAppControlMessage(AppControlType.LaunchUriResult, new LaunchUriResult()
        {
            ResponseID = request.RequestID,
            Result = E_NOTIMPL
        });

    protected virtual void OnLaunchUriResult(in LaunchUriResult result) { }
    #endregion

    #region AppService
    protected virtual void OnCallAppService(in CallAppServiceRequest request)
        => SendAppControlMessage(AppControlType.CallAppServiceResponse, new CallAppServiceResponse()
        {
            HResult = E_NOTIMPL,
        });

    protected virtual void OnCallAppServiceResponse(in CallAppServiceResponse response) { }
    #endregion

    #region Resources
    protected virtual void OnGetResource(in GetResourceRequest request)
        => SendAppControlMessage(AppControlType.GetResourceResponse, new GetResourceResponse()
        {
            HResult = E_NOTIMPL,
            ResourceData = default,
        });

    protected virtual void OnGetResourceResponse(in GetResourceResponse response) { }

    protected virtual void OnSetResource(in SetResourceRequest request)
        => SendAppControlMessage(AppControlType.SetResourceResponse, new SetResourceResponse()
        {
            HResult = E_NOTIMPL,
            ResourceData = default,
        });

    protected virtual void OnSetResourceResponse(in SetResourceResponse response) { }
    #endregion

    public void SendAppControlMessage<TMessage>(AppControlType messageType, in TMessage message)
        where TMessage : IBinaryWritable<TMessage>
    {
        Channel.SendMessage(
            new AppControlHeader()
            {
                MessageType = messageType
            },
            in message
        );
    }
}

public abstract class SystemAppControlApp(CdpChannel channel) : AppControlApp(channel), ICdpAppId
{
    static string ICdpAppId.Id { get; } = "AppControl";
    static string ICdpAppId.Name { get; } = "System";
}
