using ShortDev.Microsoft.ConnectedDevices.Messages;
using System.Buffers;

namespace ShortDev.Microsoft.ConnectedDevices.Test.Messages;

public class CommonHeaderTest
{
    [Fact]
    public void CalcSize_YieldsCorrectResult_WhenNoHeaders()
    {
        var writer = EndianWriter.Create(Endianness.BigEndian, ArrayPool<byte>.Shared);
        try
        {
            CommonHeader header = new();
            header.Write(ref writer);
            var expected = writer.Stream.WrittenSpan.Length;

            var actual = header.CalcSize();

            Assert.Equal(expected, actual);
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Fact]
    public void CalcSize_YieldsCorrectResult_WhenWithHeaders()
    {
        var writer = EndianWriter.Create(Endianness.BigEndian, ArrayPool<byte>.Shared);
        try
        {
            CommonHeader header = new()
            {
                Type = MessageType.Connect,
                AdditionalHeaders = {
                AdditionalHeader.FromUInt32(AdditionalHeaderType.Header129, 0x70_00_00_03),
                AdditionalHeader.FromUInt64(AdditionalHeaderType.PeerCapabilities, (ulong)PeerCapabilities.All),
                AdditionalHeader.FromUInt64(AdditionalHeaderType.Header131, 6u)
            }
            };
            header.Write(ref writer);
            var expected = writer.Stream.WrittenSpan.Length;

            var actual = header.CalcSize();

            Assert.Equal(expected, actual);
        }
        finally
        {
            writer.Dispose();
        }
    }
}
