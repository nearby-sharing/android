using System;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

/// <summary>
/// A value describing the message properties. <br/>
/// (See <see cref="CommonHeader.Flags"/>)
/// </summary>
[Flags]
public enum MessageFlags : short
{
    /// <summary>
    /// The caller expects ACK to be sent back to confirm that the message has been received.
    /// </summary>
    ShouldAck = 0x0001,
    /// <summary>
    /// The message contains a hashed message authentication code which will be validated by the receiver. 
    /// If not set, the HMAC field is not present. See “HMAC”.
    /// </summary>
    HasHMAC = 0x0002,
    /// <summary>
    /// If <see langword="true"/>, indicates that the message is encrypted at the session level.
    /// This is <see langword="false"/> for non-session messages (which don’t require encryption/decryption).
    /// </summary>
    SessionEncrypted = 0x0004,
    /// <summary>
    /// If <see langword="true"/>, indicates whether the remote application should be woken up.
    /// </summary>
    WakeTarget = 0x0008
}
