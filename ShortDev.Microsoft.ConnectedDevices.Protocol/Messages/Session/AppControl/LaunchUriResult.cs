using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Messages.Session.AppControl;

public sealed class LaunchUriResult : ICdpPayload<LaunchUriResult>
{
    public static LaunchUriResult Parse(BinaryReader reader)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// The HRESULT returned by the call, zero if successful.
    /// </summary>
    public required int Result { get; init; }

    /// <summary>
    /// Number corresponding to the request ID from the Launch URI message that resulted in this response. <br/>
    /// This is used to correlate requests and responses
    /// </summary>
    public required int ResponseID { get; init; }

    public void Write(BinaryWriter writer)
    {

    }
}
