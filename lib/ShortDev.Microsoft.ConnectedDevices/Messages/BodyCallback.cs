using ShortDev.IO.ValueStream;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

public delegate void BodyCallback(ref EndianWriter<HeapOutputStream> writer);
