namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Messages;

public interface ICdpHeader<T> : ICdpSerializable<T> where T : ICdpHeader<T> { }