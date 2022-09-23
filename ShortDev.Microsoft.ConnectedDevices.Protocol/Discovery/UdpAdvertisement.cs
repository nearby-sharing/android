using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery
{
    public sealed class UdpAdvertisement : IDiscovery
    {
        public UdpAdvertisement()
        {
            Initialize();
        }

        UdpClient? _advertiserClient;
        void Initialize()
        {
            _advertiserClient = new(Constants.DiscoveryPort);
            _advertiserClient.EnableBroadcast = true;
        }

        CancellationTokenSource? _advertisementCancel;
        public void StartDiscovery()
        {
            if (_advertisementCancel != null || _advertiserClient == null)
                throw new InvalidOperationException();

            // _advertiserClient.Connect(new IPEndPoint(IPAddress.Any, Constants.DiscoveryPort));
            _advertisementCancel = new();
            _ = Task.Run(async () =>
            {
                while (!_advertisementCancel.IsCancellationRequested)
                {
                    var result = await _advertiserClient.ReceiveAsync();
                    using (MemoryStream stream = new(result.Buffer))
                    using (BinaryReader reader = new(stream))
                    {
                        if (CommonHeaders.TryParse(reader, out var headers, out _) && headers != null)
                        {
                            if (headers.Type == MessageType.Discovery)
                            {
                                DiscoveryHeader discoveryHeaders = DiscoveryHeader.Parse(reader);
                            }
                        }
                    }
                }
            }, _advertisementCancel.Token);
        }

        public void StopDiscovery()
        {
            if (_advertisementCancel == null)
                throw new InvalidOperationException();

            _advertisementCancel.Cancel();
        }
    }
}
