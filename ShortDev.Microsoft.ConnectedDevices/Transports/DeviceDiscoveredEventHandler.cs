﻿using ShortDev.Microsoft.ConnectedDevices.Platforms;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public delegate void DeviceDiscoveredEventHandler(ICdpTransport sender, CdpDevice device);
