using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public class CryptorBenchmark
{
    readonly CommonHeader _header = new();
    readonly Sender _sender = new();
    readonly CdpCryptor _cryptor = new(RandomNumberGenerator.GetBytes(64));

    [Params(100, 500, MessageFragmenter.DefaultMessageFragmentSize, 20_000)]
    public int BufferSize { get; set; }

    byte[] _buffer = [];

    [GlobalSetup]
    public void Setup()
    {
        _buffer = RandomNumberGenerator.GetBytes(BufferSize);
    }

    [Benchmark]
    public void Speed()
    {
        _cryptor.EncryptMessage(_sender, _header, _buffer);
    }

    sealed class Sender : IFragmentSender
    {
        public void SendFragment(ReadOnlySpan<byte> message) { }

        public void SendFragment(ReadOnlySpan<byte> header, ReadOnlySpan<byte> payload) { }
    }
}
