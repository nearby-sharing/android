using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using System.IO;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery
{
    public class PresenceResponse
    {
        public PresenceResponse(BinaryReader reader)
        {
            ConnectionMode = (ConnectionMode)reader.ReadInt16();
            DeviceType = (DeviceType)reader.ReadInt16();
            DeviceNameLength = reader.ReadUInt16();
            DeviceName = Encoding.UTF8.GetString(reader.ReadBytes(DeviceNameLength));
            DeviceIdSalt = reader.ReadInt32();
            DeviceIdHash = reader.ReadInt32();
            PrincipalUserNameHash = reader.ReadInt32();
        }

        public ConnectionMode ConnectionMode { get; set; }

        public DeviceType DeviceType { get; set; }

        public ushort DeviceNameLength { get; set; }

        public string DeviceName { get; set; }

        public int DeviceIdSalt { get; set; }

        public int DeviceIdHash { get; set; }

        public int PrincipalUserNameHash { get; set; }
    }
}
