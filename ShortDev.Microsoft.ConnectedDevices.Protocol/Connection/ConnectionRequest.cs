using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection
{
    public sealed class ConnectionRequest
    {
        public ConnectionRequest(BinaryReader reader)
        {
            CurveType = (CurveType)reader.ReadByte();
            HMACSize = reader.ReadUInt16();
            Nonce = reader.ReadBytes(8);
            MessageFragmentSize = reader.ReadUInt32();

            var publicKeyXLength = reader.ReadUInt16();
            PublicKeyX = reader.ReadBytes(publicKeyXLength);
            var publicKeyYLength = reader.ReadUInt16();
            PublicKeyY = reader.ReadBytes(publicKeyYLength);
        }

        public CurveType CurveType { get; set; }
        public ushort HMACSize { get; set; }
        public byte[] Nonce { get; set; }
        public uint MessageFragmentSize { get; set; }
        public byte[] PublicKeyX { get; set; }
        public byte[] PublicKeyY { get; set; }

        public const int DefaultMessageFragmentSize = 16384;
    }

    public enum CurveType : byte
    {
        CT_NIST_P256_KDF_SHA512
    }
}
