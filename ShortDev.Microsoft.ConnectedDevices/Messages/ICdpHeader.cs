namespace ShortDev.Microsoft.ConnectedDevices.Messages;

public interface ICdpHeader<T> : ICdpSerializable<T> where T : ICdpHeader<T> { }