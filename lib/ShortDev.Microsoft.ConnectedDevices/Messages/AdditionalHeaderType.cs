namespace ShortDev.Microsoft.ConnectedDevices.Messages;

/// <summary>
/// If an additional header record is included, this value indicates the type. <br/>
/// Some values are implementation-specific. <br/>
/// <br/>
/// (See <see cref="AdditionalHeader.Type"/>)
/// </summary>
public enum AdditionalHeaderType : byte
{
    /// <summary>
    /// No more headers.
    /// </summary>
    None = 0,
    /// <summary>
    /// If included, the payload would contain a Next Header Size-sized ID of the message to which this message responds.
    /// </summary>
    ReplyToId,
    /// <summary>
    /// A uniquely identifiable payload meant to identify communication over devices.
    /// </summary>
    CorrelationVector,
    /// <summary>
    /// Identifies the last seen message that both participants can agree upon.
    /// </summary>
    WatermarkId,
    UserMessageRequestId = 5,

    // Extended Headers

    Header129 = 0x81,
    /// <summary>
    /// <see cref="Messages.PeerCapabilities"/>
    /// </summary>
    PeerCapabilities = 0x82,
    Header131 = 0x83
}
