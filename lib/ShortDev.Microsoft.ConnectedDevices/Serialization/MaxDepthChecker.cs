using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Bond;

/// <summary>
/// Provides utility methods for tracking recursion depth and throwing an exception when the
/// tracked depth has exceeded DeserializerControls.Active.MaxDepth.
/// </summary>
internal static class MaxDepthChecker
{
    /// <summary>The depth tracked for the current thread.</summary>
    /// <remarks>
    /// This needn't correspond 1:1 with schema structure.  It is an approximate representation of how deeply deserialization
    /// has recurred, in order to provide a defense-in-depth measure against stack overflows.
    /// </remarks>
    [ThreadStatic]
    static int t_depth;

    /// <summary>Validates the current depth against the limit, assuming it's about to be incremented, and returns it.</summary>
    /// <exception cref="InvalidDataException">Recursion depth exceeded DeserializerControls.MaxDepth.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ValidateDepthForIncrement()
    {
        int depth = t_depth;
        Debug.Assert(depth >= 0);

        // Check with >= rather than > as we're validating depth+1.
        if (depth >= DeserializerControls.Active.MaxDepth)
        {
            ThrowTooDeepException();
        }

        return depth;
    }

    /// <summary>Sets the tracked depth.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetDepth(int depth)
    {
        Debug.Assert(depth >= 0 && depth <= DeserializerControls.Active.MaxDepth);
        t_depth = depth;
    }

    /// <summary>Undoes an increment to the current depth and throws an exception indicating max depth exceeded.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowTooDeepException() =>
        throw new InvalidDataException($"Recursion depth exceeded {nameof(DeserializerControls)}.{nameof(DeserializerControls.MaxDepth)}");
}
