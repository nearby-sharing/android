namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection
{
    public enum ConnectType : byte
    {
        /// <summary>
        /// Device issued connection request
        /// </summary>
        ConnectRequest,
        /// <summary>
        /// Response to connection request
        /// </summary>
        ConnectResponse,
        /// <summary>
        /// Initial authentication (Device Level)
        /// </summary>
        DeviceAuthRequest,
        /// <summary>
        /// Response to initial authentication
        /// </summary>
        DeviceAuthResponse,
        /// <summary>
        /// Authentication of user and device combination (depending on authentication model)
        /// </summary>
        UserDeviceAuthRequest,
        /// <summary>
        /// Response to authentication of a user and device combination (depending on authentication model)
        /// </summary>
        UserDeviceAuthResponse,
        /// <summary>
        /// Authentication completed message
        /// </summary>
        AuthDoneRequest,
        /// <summary>
        /// Authentication completed response
        /// </summary>
        AuthDoneRespone,
        /// <summary>
        /// Connection failed message
        /// </summary>
        ConnectFailure,
        /// <summary>
        /// Transport upgrade request message
        /// </summary>
        UpgradeRequest,
        /// <summary>
        /// Transport upgrade response message
        /// </summary>
        UpgradeResponse,
        /// <summary>
        /// Transport upgrade finalization request message
        /// </summary>
        UpgradeFinalization,
        /// <summary>
        /// Transport upgrade finalization response message
        /// </summary>
        UpgradeFinalizationResponse,
        /// <summary>
        /// Transport details request message
        /// </summary>
        TransportRequest,
        /// <summary>
        /// Transport details response message
        /// </summary>
        TransportConfirmation,
        /// <summary>
        /// Transport upgrade failed message
        /// </summary>
        UpgradeFailure,
        /// <summary>
        /// Device information request message
        /// </summary>
        DeviceInfoMessage,
        /// <summary>
        /// Device information response message
        /// </summary>
        DeviceInfoResponseMessage
    }
}
