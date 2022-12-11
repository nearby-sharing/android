namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

internal enum NearShareControlMsgType
{
    StartTransfer = 1,
    CompleteTransfer = 2,
    FetchDataRequest = 3,
    FetchDataResponse = 4,
    CancelTransfer = 5,
    HandShakeRequest = 6,
    HandShakeResult = 7
}
