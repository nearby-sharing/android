namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Messages.Connection;

/// <summary>
/// The result of the connection request. <br/>
/// (See <see cref="ConnectionResponse.Result"/>)
/// </summary>
public enum ConnectionResult
{
    Success,
    Pending,
    Failure_Authentication,
    Failure_NotAllowed
}
