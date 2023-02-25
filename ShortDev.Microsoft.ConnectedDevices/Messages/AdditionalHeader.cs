using Microsoft.CorrelationVector;
using System;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

/// <summary>
/// 
/// (See <see cref="CommonHeader.AdditionalHeaders"/>)
/// </summary>
/// <param name="Type"></param>
/// <param name="Value"></param>
public record AdditionalHeader(AdditionalHeaderType Type, ReadOnlyMemory<byte> Value)
{
    public static AdditionalHeader CreateCorrelationHeader()
        => FromCorrelationVector(new CorrelationVector());

    public static AdditionalHeader FromCorrelationVector(CorrelationVector cv)
        => FromCorrelationVector(cv.ToString());

    public static AdditionalHeader FromCorrelationVector(string cv)
    {
        return new(
            AdditionalHeaderType.CorrelationVector,
            Encoding.ASCII.GetBytes(cv)
        );
    }
}
