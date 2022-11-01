using System;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Serialization;

public partial class UUID
{
    public unsafe Guid ToGuid()
    {
        Span<byte> buffer = stackalloc byte[16];
        fixed (byte* pBuffer = buffer)
        {
            *(uint*)pBuffer = Data1;
            *(ushort*)(pBuffer + 4) = Data2;
            *(ushort*)(pBuffer + 6) = Data3;
            *(ulong*)(pBuffer + 8) = Data4;
        }
        return new(buffer);
    }

    public static unsafe UUID FromGuid(Guid value)
    {
        Span<byte> buffer = stackalloc byte[16];
        if (!value.TryWriteBytes(buffer))
            throw new Exception("Could not convert");

        fixed (byte* pBuffer = buffer)
        {
            return new()
            {
                Data1 = *(uint*)pBuffer,
                Data2 = *(ushort*)(pBuffer + 4),
                Data3 = *(ushort*)(pBuffer + 6),
                Data4 = *(ulong*)(pBuffer + 8)
            };
        }
    }

    public override string ToString()
        => ToGuid().ToString();
}