using System;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms
{
    public sealed class CdpRfcommOptions
    {
        public string? ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public Action<CdpRfcommSocket>? OnSocketConnected { get; set; }
    }
}
