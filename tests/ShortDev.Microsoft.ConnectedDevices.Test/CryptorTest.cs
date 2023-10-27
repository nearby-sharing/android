using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Networking;
using System.Linq;
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

        EndianWriter writer = new(Endianness.BigEndian);
        cryptor.EncryptMessage(writer, header, payload);

        EndianReader reader = new(Endianness.BigEndian, writer.Buffer.AsSpan());

        header = CommonHeader.Parse(ref reader);
        var readerContent = reader.ReadToEnd();

        var encryptedPayload = readerContent[..^Constants.HMacSize];
        var hmac = readerContent[^Constants.HMacSize..];

        var decrypted = cryptor.DecryptMessage(header, encryptedPayload, hmac).Span;

        Assert.True(payload.SequenceEqual(decrypted[sizeof(uint)..]));
    }
}
