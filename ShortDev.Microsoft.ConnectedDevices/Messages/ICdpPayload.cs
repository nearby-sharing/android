namespace ShortDev.Microsoft.ConnectedDevices.Messages;

public interface ICdpPayload<T> : ICdpSerializable<T> where T : ICdpPayload<T> { }