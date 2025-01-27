using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.NearShare.Messages;
using ShortDev.Microsoft.ConnectedDevices.Serialization;

namespace ShortDev.Microsoft.ConnectedDevices.Test;

public sealed class SerializationTest(ITestOutputHelper output)
{
    static SerializationTest()
    {
        TestValueGenerator.TryRegisterTypeFactory(() => ConnectedDevicesPlatform.CreateDeviceCertificate(CdpEncryptionParams.Default));
    }

    private readonly ITestOutputHelper _output = output;

    public static IEnumerable<TheoryDataRow<Type>> GenerateMsgTypes()
    {
        var assembly = typeof(CommonHeader).Assembly;
        foreach (var type in assembly.DefinedTypes)
        {
            if (
                IsOk(typeof(ICdpHeader<>), type) ||
                IsOk(typeof(ICdpPayload<>), type) &&
                type.Name != "PresenceResponse"
            )
                yield return type;
        }

        static bool IsOk(Type TInterface, Type TClass)
        {
            foreach (var item in TClass.GetInterfaces())
            {
                if (item.IsGenericType && item.GetGenericTypeDefinition() == TInterface)
                    return true;
            }
            return false;
        }
    }

    [Theory]
    [MemberData(nameof(GenerateMsgTypes))]
    public void ParseMessage_ShouldYieldSameAsWritten(Type type)
    {
        var testMsg = TestRun<CommonHeader>;
        var genericDefinition = testMsg.Method.GetGenericMethodDefinition();
        var genericMethod = genericDefinition.MakeGenericMethod(type);

        genericMethod.Invoke(null, [Endianness.LittleEndian]);
        genericMethod.Invoke(null, [Endianness.BigEndian]);

        static void TestRun<T>(Endianness endianness) where T : ICdpSerializable<T>
        {
            Type type = typeof(T);

            // allocate
            var instance = TestValueGenerator.RandomValue<T>();

            // write - 1st pass
            EndianWriter writer = new(endianness);
            instance.Write(writer);
            var writtenMemory1 = writer.Buffer.AsMemory();

            // parse
            EndianReader reader = new(endianness, writtenMemory1.Span);
            var parsedObject = T.Parse(ref reader);

            // write - 2nd pass
            writer = new(endianness);
            parsedObject.Write(writer);
            var writtenMemory2 = writer.Buffer.AsMemory();

            // assert
            Assert.True(writtenMemory1.Span.SequenceEqual(writtenMemory2.Span));
        }
    }

    [Fact]
    public void ValueSet()
    {
        ValueSet response = new();
        response.Add("ControlMessage", (uint)NearShareControlMsgType.FetchDataResponse);
        response.Add("ContentId", (uint)1);
        response.Add("BlobPosition", (ulong)2);
        response.Add("DataBlob", (List<byte>)[42]);

        EndianWriter writer1 = new(Endianness.BigEndian);
        response.Write(writer1);

        EndianWriter writer2 = new(Endianness.BigEndian);
        FetchDataResponse.Write(writer2, 1, 2, length: 1, out var blob);
        blob[0] = 42;

        Assert.Equal(1, blob.Length);
        Assert.Equal(writer1.Buffer.ToArray(), writer2.Buffer.ToArray());
    }
}