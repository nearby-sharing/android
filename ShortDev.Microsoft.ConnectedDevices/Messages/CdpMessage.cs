using ShortDev.Networking;
using System;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

public sealed class CdpMessage : IDisposable
{
    readonly EndianBuffer _buffer;

    public CdpMessage(CommonHeader header)
    {
        Header = header;
        _buffer = new(header.FragmentCount * Constants.DefaultMessageFragmentSize);
    }

    public CdpMessage(CommonHeader header, byte[] payload)
    {
        Header = header;
        _buffer = new(payload);
        _receivedFragmentCount = header.FragmentCount;
    }

    public CommonHeader Header { get; }

    public uint Id
        => Header.SequenceNumber;

    #region Fragments
    public bool IsComplete
        => _receivedFragmentCount >= Header.FragmentCount;

    ushort _receivedFragmentCount = 0;
    public void AddFragment(ReadOnlySpan<byte> fragment)
    {
        if (IsComplete)
            throw new InvalidOperationException("Already received all fragments");

        _buffer.Write(fragment);
        _receivedFragmentCount++;
    }
    #endregion

    public EndianReader Read()
    {
        if (!IsComplete)
            throw new InvalidOperationException("Wait for completion");

        return new(Endianness.BigEndian, _buffer.AsSpan());
    }

    public EndianReader Read(out byte[] prepend)
    {
        if (!IsComplete)
            throw new InvalidOperationException("Wait for completion");

        var reader = Read();
        prepend = reader.ReadBytes(0x0000000C).ToArray();
        return reader;
    }

    public void Dispose()
    {
    }
}
