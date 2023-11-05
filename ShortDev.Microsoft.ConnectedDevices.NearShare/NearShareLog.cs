using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare;

internal static partial class NearShareLog
{
    [LoggerMessage(EventId = 501, Level = LogLevel.Information, Message = "Receiving file {FileNames} from session {SessionId:X} via {TransportType}")]
    public static partial void ReceivingFile(this ILogger logger, IEnumerable<string> fileNames, ulong sessionId, CdpTransportType transportType);

    [LoggerMessage(EventId = 502, Level = LogLevel.Information, Message = "Received uri {Uri} from session {SessionId:X} via {TransportType}")]
    public static partial void ReceivedUrl(this ILogger logger, string uri, ulong sessionId, CdpTransportType transportType);
}
