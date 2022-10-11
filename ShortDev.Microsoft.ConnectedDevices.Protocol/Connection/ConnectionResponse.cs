using System.IO;
using ShortDev.Networking;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;

public sealed class ConnectionResponse : ICdpPayload<ConnectionResponse>
{
    public required ConnectionResult Result { get; set; }
    public required ushort HMACSize { get; set; }
    public required byte[] Nonce { get; set; }
    public required uint MessageFragmentSize { get; set; }
    public required byte[] PublicKeyX { get; set; }
    public required byte[] PublicKeyY { get; set; }

    public static ConnectionResponse Parse(BinaryReader reader)
    {
        var result = (ConnectionResult)reader.ReadByte();
        return new()
        {
            Result = result,
            HMACSize = reader.ReadUInt16(),
            Nonce = reader.ReadBytes(Constants.NonceLength),
            MessageFragmentSize = reader.ReadUInt32(),
            PublicKeyX = reader.ReadBytesWithLength(),
            PublicKeyY = reader.ReadBytesWithLength()
        };
    }

    public void Write(BinaryWriter writer)
    {
        if (Nonce.Length != Constants.NonceLength)
            throw new InvalidDataException($"{nameof(Nonce)} has to be {Constants.NonceLength} bytes long");

        writer.Write((byte)Result);
        writer.Write((ushort)HMACSize);
        writer.Write(Nonce);
        writer.Write((uint)MessageFragmentSize);

        writer.Write((ushort)PublicKeyX.Length);
        writer.Write(PublicKeyX);
        writer.Write((ushort)PublicKeyY.Length);
        writer.Write(PublicKeyY);
    }
}
