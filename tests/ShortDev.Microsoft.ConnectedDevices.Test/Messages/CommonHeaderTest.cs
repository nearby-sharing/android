using ShortDev.Microsoft.ConnectedDevices.Messages;
using System.Buffers;

namespace ShortDev.Microsoft.ConnectedDevices.Test.Messages;

public class CommonHeaderTest
{
    [Fact]
    public void CalcSize_YieldsCorrectResult_WhenNoHeaders()
    {
        using EndianWriter writer = EndianWriter.Create(Endianness.BigEndian, ArrayPool<byte>.Shared);

        CommonHeader header = new();
        header.Write(writer);
        var expected = writer.Buffer.WrittenSpan.Length;

        var actual = header.CalcSize();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CalcSize_YieldsCorrectResult_WhenWithHeaders()
    {
        using EndianWriter writer = EndianWriter.Create(Endianness.BigEndian, ArrayPool<byte>.Shared);

        CommonHeader header = new()
        {
            Type = MessageType.Connect,
            AdditionalHeaders = {
                AdditionalHeader.FromUInt32(AdditionalHeaderType.Header129, 0x70_00_00_03),
                AdditionalHeader.FromUInt64(AdditionalHeaderType.PeerCapabilities, (ulong)PeerCapabilities.All),
                AdditionalHeader.FromUInt64(AdditionalHeaderType.Header131, 6u)
            }
        };
        header.Write(writer);
        var expected = writer.Buffer.WrittenSpan.Length;

        var actual = header.CalcSize();

        Assert.Equal(expected, actual);
    }
}
