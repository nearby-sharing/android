namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

public interface ICdpHeader<T> : ICdpSerializable<T> where T : ICdpHeader<T> { }