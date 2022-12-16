namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Messages;

public interface ICdpPayload<T> : ICdpSerializable<T> where T : ICdpPayload<T> { }