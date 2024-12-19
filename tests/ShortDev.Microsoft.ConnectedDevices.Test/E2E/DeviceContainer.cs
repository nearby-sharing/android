using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Collections.Concurrent;

namespace ShortDev.Microsoft.ConnectedDevices.Test.E2E;

internal sealed class DeviceContainer
{
    readonly ConcurrentDictionary<Device, List<Adverstisement>> _registry = [];
    sealed record Entry(Device Device, List<Adverstisement> Adverstisements);

    public Device? FindDevice(string address)
        => _registry.FirstOrDefault(x => x.Key.Address == address).Key;

    public void Advertise(Device device, uint manufacturer, ReadOnlyMemory<byte> data)
    {
        var list = _registry.GetOrAdd(device, static key => []);
        lock (list)
        {
            list.Add(new(manufacturer, data));
        }
        FoundDevice?.Invoke(device, new(manufacturer, data));
    }

    public bool TryRemove(Device device)
        => _registry.Remove(device, out _);

    public event Action<Device, Adverstisement>? FoundDevice;

    public sealed record Adverstisement(uint Manufacturer, ReadOnlyMemory<byte> Data);
    public sealed record Device(CdpTransportType TransportType, string Address)
    {
        public CdpSocket ConnectFrom(EndpointInfo client)
        {
            (Stream Input, Stream Output)? stream = null;
            ConnectionRequest?.Invoke(client, ref stream);

            if (stream is null)
                throw new InvalidOperationException("Server did not accept");

            return new CdpSocket()
            {
                InputStream = stream.Value.Input,
                OutputStream = stream.Value.Output,
                Endpoint = new(TransportType, Address, "Some Service Id"),
                Close = () =>
                {
                    stream.Value.Output.Dispose();
                    stream.Value.Input.Dispose();
                }
            };
        }

        public event ConnectionRequestHandler? ConnectionRequest;

        public delegate void ConnectionRequestHandler(EndpointInfo client, ref (Stream Input, Stream Output)? stream);
    }
}
