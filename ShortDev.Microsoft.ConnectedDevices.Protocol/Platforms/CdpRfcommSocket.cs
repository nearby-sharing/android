using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms
{
    public sealed class CdpRfcommSocket
    {
        public Stream? InputStream { get; set; }
        public Stream? OutputStream { get; set; }

        public CdpBluetoothDevice? RemoteDevice { get; set; }
        public Action? Close { get; set; }
    }
}
