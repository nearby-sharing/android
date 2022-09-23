namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

public interface ICdpPayload<T> : ICdpSerializable<T> where T : ICdpPayload<T> { }