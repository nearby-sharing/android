using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;

public sealed class ConnectionResponse
{
    public ConnectionResponse(BinaryReader reader)
    {
        Result = (ConnectionResult)reader.ReadByte();
        HMACSize = reader.ReadUInt16();
        Nonce = reader.ReadBytes(8);
        MessageFragmentSize = reader.ReadUInt32();

        var publicKeyXLength = reader.ReadUInt16();
        PublicKeyX = reader.ReadBytes(publicKeyXLength);
        var publicKeyYLength = reader.ReadUInt16();
        PublicKeyY = reader.ReadBytes(publicKeyYLength);
    }

    public ConnectionResult Result { get; set; }
    public ushort HMACSize { get; set; }
    public byte[] Nonce { get; set; }
    public uint MessageFragmentSize { get; set; }
    public byte[] PublicKeyX { get; set; }
    public byte[] PublicKeyY { get; set; }
}
