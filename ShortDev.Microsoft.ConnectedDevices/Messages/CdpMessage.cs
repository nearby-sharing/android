using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
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

    public SessionFragmentHeader? FragmentHeader { get; private set; }

    public EndianReader Read()
    {
        if (!IsComplete)
            throw new InvalidOperationException("Wait for completion");

        EndianReader reader = new(Endianness.BigEndian, _buffer.AsSpan());
        FragmentHeader = SessionFragmentHeader.Parse(reader);
        return reader;
    }

    public void Dispose()
    {
    }
}
