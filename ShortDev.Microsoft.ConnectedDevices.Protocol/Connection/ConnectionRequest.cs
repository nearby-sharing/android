using ShortDev.Networking;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;

public sealed class ConnectionRequest : ICdpPayload<ConnectionRequest>
{
    public required CurveType CurveType { get; set; }
    public required ushort HMACSize { get; set; }
    public required byte[] Nonce { get; set; }
    public required uint MessageFragmentSize { get; set; }
    public required byte[] PublicKeyX { get; set; }
    public required byte[] PublicKeyY { get; set; }

    public static ConnectionRequest Parse(BinaryReader reader)
        => new()
        {
            CurveType = (CurveType)reader.ReadByte(),
            HMACSize = reader.ReadUInt16(),
            Nonce = reader.ReadBytes(Constants.NonceLength),
            MessageFragmentSize = reader.ReadUInt32(),
            PublicKeyX = reader.ReadBytesWithLength(),
            PublicKeyY = reader.ReadBytesWithLength()
        };

    public void Write(BinaryWriter writer)
    {
        if (Nonce.Length != Constants.NonceLength)
            throw new InvalidDataException($"{nameof(Nonce)} has to be {Constants.NonceLength} bytes long");

        writer.Write((byte)CurveType);
        writer.Write((ushort)HMACSize);
        writer.Write(Nonce);
        writer.Write((uint)MessageFragmentSize);

        writer.Write((ushort)PublicKeyX.Length);
        writer.Write(PublicKeyX);
        writer.Write((ushort)PublicKeyY.Length);
        writer.Write(PublicKeyY);
    }
}