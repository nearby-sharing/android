namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

public enum ControlMessageType : byte
{
    StartChannelRequest = 0,
    StartChannelResponse = 1,
    EnumerateAppsReponse = 4,
    EnumerateAppTargetNamesRequest = 5,
    EnumerateAppTargetNamesResponse = 6
}
