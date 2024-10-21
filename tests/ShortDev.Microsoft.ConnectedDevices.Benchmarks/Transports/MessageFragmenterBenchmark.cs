using BenchmarkDotNet.Engines;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

[SimpleJob(RunStrategy.Throughput)]
public class MessageFragmenterBenchmark
{
    readonly CommonHeader _header = new();
    readonly IFragmentSender _copySender = new CopyFragmentSender();
    readonly IFragmentSender _concatSender = new ConcatFragmentSender();
    readonly IFragmentSender _nocopySender = new NoCopyFragmentSender();

    [Params(20_000, 40_000, 60_000)]
    public int BufferSize { get; set; }

    byte[] _buffer = [];

    [GlobalSetup]
    public void Setup()
    {
        _buffer = RandomNumberGenerator.GetBytes(BufferSize);
    }

    [Benchmark]
    public void FragmentWithCopy()
    {
        _copySender.SendMessage(_header, _buffer);
    }

    [Benchmark]
    public void FragmentWithConcatination()
    {
        _concatSender.SendMessage(_header, _buffer);
    }

    [Benchmark]
    public void FragmentWithoutCopy()
    {
        _nocopySender.SendMessage(_header, _buffer);
    }

    sealed class CopyFragmentSender : IFragmentSender
    {
        public void SendFragment(ReadOnlySpan<byte> message) { }

        public void SendFragment(ReadOnlySpan<byte> header, ReadOnlySpan<byte> payload)
        {
            EndianWriter writer = new(Endianness.BigEndian);
            writer.Write(header);
            writer.Write(payload);
            SendFragment(writer.Buffer.AsSpan());
        }
    }

    sealed class ConcatFragmentSender : IFragmentSender
    {
        public void SendFragment(ReadOnlySpan<byte> message) { }

        public void SendFragment(ReadOnlySpan<byte> header, ReadOnlySpan<byte> payload)
            => SendFragment([.. header, .. payload]);
    }

    sealed class NoCopyFragmentSender : IFragmentSender
    {
        public void SendFragment(ReadOnlySpan<byte> message) { }

        public void SendFragment(ReadOnlySpan<byte> header, ReadOnlySpan<byte> payload) { }
    }
}
