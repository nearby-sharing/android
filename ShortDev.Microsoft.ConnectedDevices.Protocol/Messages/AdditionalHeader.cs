namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Messages;

/// <summary>
/// 
/// (See <see cref="CommonHeader.AdditionalHeaders"/>)
/// </summary>
/// <param name="Type"></param>
/// <param name="Value"></param>
public record AdditionalHeader(AdditionalHeaderType Type, byte[] Value);
