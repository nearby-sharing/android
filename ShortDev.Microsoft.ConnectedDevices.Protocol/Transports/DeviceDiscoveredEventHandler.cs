using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Transports;

public delegate void DeviceDiscoveredEventHandler(ICdpTransport sender, CdpDevice device, CdpAdvertisement advertisement);
