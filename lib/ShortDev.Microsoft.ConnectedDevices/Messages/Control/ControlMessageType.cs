namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

public enum ControlMessageType : byte
{
    StartChannelRequest = 0,
    StartChannelResponse = 1,
    StopChannelRequest = 2,
    EnumerateAppsRequest = 3,
    EnumerateAppsReponse = 4,
    EnumerateAppTargetNamesRequest = 5,
    EnumerateAppTargetNamesResponse = 6,
    ChannelAuthorizationRequest = 7,
    ChannelAuthorizationResponse = 8,
}
