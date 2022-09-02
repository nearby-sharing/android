namespace ShortDev.Microsoft.ConnectedDevices.Protocol
{
    public enum NextHeaderType : byte
    {
        /// <summary>
        /// No more headers.
        /// </summary>
        None = 0,
        /// <summary>
        /// If included, the payload would contain a Next Header Size-sized ID of the message to which this message responds.
        /// </summary>
        ReplyTold,
        /// <summary>
        /// A uniquely identifiable payload meant to identify communication over devices.
        /// </summary>
        CorrelationVector,
        /// <summary>
        /// Identifies the last seen message that both participants can agree upon.
        /// </summary>
        WatermarkId
    }
}
