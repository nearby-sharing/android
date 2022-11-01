using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Serialization;

public partial class ValueSet
{
    public static ValueSet Parse(byte[] data)
    {
        using (MemoryStream stream = new(data))
            return Parse(stream);
    }

    public static ValueSet Parse(Stream stream)
    {
        CompactBinaryReader<InputStream> reader = new(new(stream));
        return Deserialize<ValueSet>.From(reader);
    }

    /// <summary>
    /// Get's the value of the specified key <br/>
    /// Throws if the key is not found
    /// </summary>
    public T GetValue<T>(string key)
        => Entries[key].Get<T>();
}