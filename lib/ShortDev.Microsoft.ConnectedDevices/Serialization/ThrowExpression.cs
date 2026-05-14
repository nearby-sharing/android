using System.Runtime.CompilerServices;

namespace Bond.Expressions;

/// <summary>
/// ThrowExpression is a utility that makes it easy to create expressions that throw exceptions.
/// </summary>
internal static class ThrowExpression
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidTypeException(BondDataType actualType)
    {
        throw new InvalidDataException(string.Format("Invalid type {0}", actualType));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidTypeException(BondDataType expectedType, BondDataType actualType)
    {
        throw new InvalidDataException(string.Format("Invalid type {0}, expected {1}", actualType, expectedType));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidDataException(string message)
    {
        throw new InvalidDataException(message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowRequiredFieldMissingException(string schema, string field)
    {
        throw new InvalidDataException(string.Format("Required field {0}.{1} missing", schema, field));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowRequiredFieldsMissingException(List<string> names, int index)
    {
        throw new InvalidDataException(string.Format("Required field {0} missing", names[index]));
    }
}
