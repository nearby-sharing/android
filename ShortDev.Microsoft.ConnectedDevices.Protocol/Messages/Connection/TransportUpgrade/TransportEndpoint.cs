using System.IO;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Messages.Connection.TransportUpgrade;

public record class TransportEndpoint(CdpTransportType Type, byte[] Data) : ICdpPayload<TransportEndpoint>, ICdpArraySerializable<TransportEndpoint>
{
    public static TransportEndpoint Tcp { get; } = new(CdpTransportType.Tcp, new byte[0]);

    public static TransportEndpoint Parse(BinaryReader reader)
    {
        var type = (CdpTransportType)reader.ReadUInt16();
        var length = reader.ReadInt32();
        var data = reader.ReadBytes(length);
        return new(type, data);
    }

    public static TransportEndpoint[] ParseArray(BinaryReader reader)
    {
        var arrayLength = reader.ReadUInt16();
        var endpoints = new TransportEndpoint[arrayLength];
        for (int i = 0; i < arrayLength; i++)
            endpoints[i] = Parse(reader);
        return endpoints;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((ushort)Type);
        writer.Write((uint)Data.Length);
        writer.Write(Data);
    }

    public static void WriteArray(BinaryWriter writer, TransportEndpoint[] endpoints)
    {
        writer.Write((ushort)endpoints.Length);
        foreach (var endpoint in endpoints)
            endpoint.Write(writer);
    }
}
