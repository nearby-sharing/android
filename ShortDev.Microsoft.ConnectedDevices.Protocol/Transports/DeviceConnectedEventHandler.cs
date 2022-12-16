using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Transports;

public delegate void DeviceConnectedEventHandler(ICdpTransport sender, CdpSocket socket);
