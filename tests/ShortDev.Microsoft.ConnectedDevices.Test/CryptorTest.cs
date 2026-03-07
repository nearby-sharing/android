using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Test;

public sealed class CryptorTest
{
    static CdpCryptor CreateCryptor()
        => new(RandomNumberGenerator.GetBytes(64));

    [Fact]
    public void Decrypt_ShouldYieldSameAsEncrypt()
    {
        CdpCryptor cryptor = CreateCryptor();

        var header = TestValueGenerator.RandomValue<CommonHeader>();
        ReadOnlySpan<byte> payload = TestValueGenerator.RandomValue<byte[]>();

        FragmentSenderSpy fragmentSender = new();
        cryptor.EncryptMessage(fragmentSender, header, payload);
        Assert.NotNull(fragmentSender.Fragment);

        var reader = EndianReader.FromSpan(Endianness.BigEndian, fragmentSender.Fragment.Value.Span);

        header = CommonHeader.Parse(ref reader);
        var readerContent = fragmentSender.Fragment.Value.Span[(int)reader.Stream.Position..];

        var encryptedPayload = readerContent[..^Constants.HMacSize];
        var hmac = readerContent[^Constants.HMacSize..];

        var decrypted = cryptor.DecryptMessage(header, encryptedPayload, hmac).Span;

        Assert.True(payload.SequenceEqual(decrypted));
    }

    sealed class FragmentSenderSpy : IFragmentSender
    {
        public ReadOnlyMemory<byte>? Fragment { get; private set; }
        public void SendFragment(ReadOnlySpan<byte> fragment)
        {
            Assert.Null(Fragment);

            Fragment = fragment.ToArray();
        }
    }
}
