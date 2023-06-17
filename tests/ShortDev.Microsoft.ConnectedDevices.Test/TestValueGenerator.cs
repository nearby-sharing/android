using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Test;

internal static class TestValueGenerator
{
    public static T RandomValue<T>()
        => (T)RandomValue(typeof(T));

    public static object RandomValue(Type type)
        => RandomValueInternal(type, depth: 0);

    static readonly Dictionary<Type, Func<object>> _factories = new();
    public static bool TryRegisterTypeFactory<T>(Func<T> factory)
    {
        var type = typeof(T);

        var result = _factories.TryAdd(
            type,
            () => factory() ?? throw new NullReferenceException($"Factory for type \"{type}\" returned \"null\"")
        );
        return result;
    }


    static object RandomValueInternal(Type type, ulong depth)
    {
        ThrowOnStackOverflow(depth);

        if (type.IsEnum)
            return RandomValueInternal(type.GetEnumUnderlyingType(), depth + 1);

        if (type.IsPrimitive)
        {
            var abc = RandomPrimitive<int>;
            var genericMethodDefinition = abc.Method.GetGenericMethodDefinition();
            var genericMethod = genericMethodDefinition.MakeGenericMethod(type);
            return genericMethod.Invoke(null, null) ?? throw new NullReferenceException($"No result for \"{nameof(RandomPrimitive)}\"");
        }

        if (type == typeof(string))
        {
            var strLen = RandomNumberGenerator.GetInt32(10);

            Span<byte> buffer = stackalloc byte[strLen];
            RandomNumberGenerator.Fill(buffer);

            return Encoding.UTF8.GetString(buffer);
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType() ?? throw new NullReferenceException($"Could not get element-type of type \"{type}\"");
            return RandomArray(elementType, depth + 1);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
        {
            var elementType = type.GenericTypeArguments[0];
            return RandomArray(elementType, depth + 1);
        }

        return RandomObject(type, depth + 1);
    }

    unsafe static T RandomPrimitive<T>() where T : unmanaged
    {
        Span<byte> buffer = stackalloc byte[sizeof(T)];
        RandomNumberGenerator.Fill(buffer);

        fixed (byte* pBuffer = buffer)
            return Unsafe.AsRef<T>(pBuffer);
    }

    static Array RandomArray(Type elementType, ulong depth)
    {
        var arrayLen = RandomNumberGenerator.GetInt32(10);
        var array = Array.CreateInstance(elementType, arrayLen);
        for (int i = 0; i < arrayLen; i++)
        {
            var value = RandomValueInternal(elementType, depth + 1);
            array.SetValue(value, i);
        }
        return array;
    }

    static object RandomObject(Type type, ulong depth)
    {
        if (type.IsPrimitive)
            throw new ArgumentException("Expected non-primitive type");

        if (IsStackOverflow(depth))
            return null!;

        // allocate
        var instance = RuntimeHelpers.GetUninitializedObject(type);

        if (_factories.TryGetValue(type, out var factory))
            return factory();

        if (type.Namespace?.StartsWith("ShortDev") != true)
            return instance;

        // initialize
        foreach (var prop in type.GetProperties())
        {
            if (!prop.CanWrite)
                continue;

            var value = RandomValueInternal(prop.PropertyType, depth + 1);
            prop.SetValue(instance, value);
        }

        return instance;
    }

    static void ThrowOnStackOverflow(ulong depth)
    {
        if (IsStackOverflow(depth))
            throw new StackOverflowException();
    }

    static bool IsStackOverflow(ulong depth)
        => depth >= 20;
}
