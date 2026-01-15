using ShortDev.Microsoft.ConnectedDevices.Messages;

namespace ShortDev.Microsoft.ConnectedDevices.Test.Messages;

public class CommonHeaderTest
{
    [Fact]
    public void CalcSize_YieldsCorrectResult_WhenNoHeaders()
    {
        EndianWriter writer = new(Endianness.BigEndian);

        CommonHeader header = new();
        header.Write(writer);
        var expected = writer.Buffer.Size;

        var actual = header.CalcSize();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CalcSize_YieldsCorrectResult_WhenWithHeaders()
    {
        EndianWriter writer = new(Endianness.BigEndian);

        CommonHeader header = new()
        {
            Type = MessageType.Connect,
            AdditionalHeaders = {
                AdditionalHeader.FromUInt32(AdditionalHeaderType.ChannelHostSettings, 0x70_00_00_03),
                AdditionalHeader.FromUInt64(AdditionalHeaderType.PeerCapabilities, (ulong)PeerCapabilities.All),
                AdditionalHeader.FromUInt64(AdditionalHeaderType.Header131, 6u)
            }
        };
        header.Write(writer);
        var expected = writer.Buffer.Size;

        var actual = header.CalcSize();

        Assert.Equal(expected, actual);
    }
}
