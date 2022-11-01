namespace ShortDev.Microsoft.ConnectedDevices.Protocol.NearShare;

public enum ControlMessageType
{
    StartRequest = 1,
    StartResponse = 2,
    FetchDataRequest = 3,
    FetchDataResponse = 4,
    Cancel = 5,
    HandShakeRequest = 6,
    HandShakeResult = 7
}
