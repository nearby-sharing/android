using ShortDev.Microsoft.ConnectedDevices.Platforms;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public delegate void DeviceConnectedEventHandler(ICdpTransport sender, CdpSocket socket);
