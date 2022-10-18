using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Networking;
using Spectre.Console;
using System.Security.Cryptography;
using System.Text.Json;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Cryptography;

var secret = BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Secret"));
var data = BinaryConvert.ToBytes(AnsiConsole.Ask<string>("Message"));

var encryptionHelper = CdpEncryptionHelper.FromSecret(secret);
using (MemoryStream stream = new(data))
using (BigEndianBinaryReader reader = new(stream))
{
    CommonHeader header = CommonHeader.Parse(reader);
    Console.WriteLine(BinaryConvert.ToString(encryptionHelper.DecyptMessage(header, reader.ReadBytes(header.PayloadSize))));
}

return;

JsonSerializerOptions options = new()
{
    IncludeFields = true
};

var algo = OpenAlgorithm();

var localParams = JsonSerializer.Deserialize<ECParameters>(AnsiConsole.Ask<string>("Local"), options);
localParams.Curve = ECCurve.NamedCurves.nistP256;
var input = ECDsa.Create(localParams).ExportECPrivateKey();
var privateKey = ImportPrivateKey(algo, input);



static unsafe BCRYPT_ALG_HANDLE OpenAlgorithm()
{
    BCRYPT_ALG_HANDLE alogrithm = new();
    fixed (char* pStr = "ECDH_P256")
    {
        if (PInvoke.BCryptOpenAlgorithmProvider(&alogrithm, new PCWSTR(pStr), new PCWSTR(), 0) < 0)
            throw new Exception("Failed to open an algorithm provider");
    }
    return alogrithm;
}

static BCRYPT_KEY_HANDLE ImportPrivateKey(BCRYPT_ALG_HANDLE algorithm, Span<byte> input)
    => ImportKey(algorithm, input, "ECCPRIVATEBLOB");

static BCRYPT_KEY_HANDLE ImportPublicKey(BCRYPT_ALG_HANDLE algorithm, Span<byte> input)
    => ImportKey(algorithm, input, "ECCPUBLICBLOB");

static unsafe BCRYPT_KEY_HANDLE ImportKey(BCRYPT_ALG_HANDLE algorithm, Span<byte> input, string type)
{
    BCRYPT_KEY_HANDLE result = new();
    fixed (byte* pInput = input)
    fixed (char* pStr = type)
    {
        NTSTATUS status = NTSTATUS.STATUS_SUCCESS;
        if ((status = PInvoke.BCryptImportKeyPair(algorithm, default(BCRYPT_KEY_HANDLE), new PCWSTR(pStr), &result, pInput, (uint)input.Length, 0u)) < 0)
            throw new Exception("Failed importing key");
    }
    return result;
}

static unsafe void Encrypt(BCRYPT_KEY_HANDLE key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> plainText, Span<byte> output)
{
    if (iv.Length > 16)
        throw new ArgumentException("Initialization vector must not exceed the size of a block");

    Span<byte> ivBuffer = stackalloc byte[16];
    for (int i = 0; i < iv.Length; i++)
        ivBuffer[i] = iv[i];

    uint result = 0;

    fixed (byte* pIV = ivBuffer)
    fixed (byte* pDate = plainText)
    fixed (byte* pOutput = output)
    {
        if (PInvoke.BCryptEncrypt(key, pDate, (uint)plainText.Length, (void*)0, pIV, 0x10, pOutput, (uint)output.Length, &result, 0) < 0)
            throw new Exception("Failed to encrypt the data");

        if (output.Length != result)
            throw new Exception("Encrypted data is the wrong length");
    }
}

static unsafe void SecretExchange(BCRYPT_KEY_HANDLE privateKey, BCRYPT_KEY_HANDLE publicKey, Span<byte> output)
{
    BCRYPT_SECRET_HANDLE hAgreedSecret = new();
    if (PInvoke.BCryptSecretAgreement(privateKey, publicKey, &hAgreedSecret, 0u) < 0)
        throw new Exception("Failed to compute secret");

    uint result = 0;

    BCryptBufferDesc parameterList = new();
    fixed (byte* pOutput = output)
    fixed (char* pStr = "HMAC") // HASH
    {
        uint cbDerivedKey = 64;
        if (PInvoke.BCryptDeriveKey(hAgreedSecret, new PCWSTR(pStr), &parameterList, pOutput, cbDerivedKey, &result, 0) < 0)
            throw new Exception("Failed to derive key");

        if (output.Length != result)
            throw new Exception("Secret is not the correct length");

        PInvoke.BCryptDestroySecret(hAgreedSecret);
    }
}

//return;

//var adapter = await BluetoothAdapter.GetDefaultAsync();
//Debug.Print(adapter.BluetoothAddress.ToString("X"));

//Console.Write("Hex: ");
//var hex = Console.ReadLine();
//if (hex == null)
//    throw new ArgumentNullException(nameof(hex));

//BinaryConvert.AsBytes(hex, out var length, null);
//byte[] buffer = new byte[length];
//BinaryConvert.AsBytes(hex, out _, buffer);

//using (MemoryStream stream = new(buffer))
//using (BigEndianBinaryReader reader = new(stream))
//{
//    if (!CommonHeader.TryParse(reader, out var header, out _) || header == null)
//        throw new InvalidDataException();

//    foreach (var entry in header.AdditionalHeaders)
//    {
//        Debug.Print(entry.Type + " " + BinaryConvert.ToString(entry.Value));
//    }

//    if (header.Type == MessageType.Connect)
//    {
//        ConnectionHeader connectionHeader = ConnectionHeader.Parse(reader);
//        switch (connectionHeader.ConnectMessageType)
//        {
//            case ConnectionType.ConnectRequest:
//                {
//                    ConnectionRequest msg = ConnectionRequest.Parse(reader);
//                    break;
//                }
//            case ConnectionType.ConnectResponse:
//                {
//                    ConnectionResponse msg = ConnectionResponse.Parse(reader);
//                    break;
//                }
//            default:
//                throw new NotImplementedException();
//        }
//    }
//    else
//        throw new NotImplementedException();

//    // DiscoveryHeaders discoveryHeaders = new(reader);
//    // PresenceResponse presenceResponse = new(reader);
//    reader.ReadByte();
//}