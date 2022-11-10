using ShortDev.Networking;
using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

public sealed class CdpMessage : IDisposable
{
    MemoryStream _stream;
    BigEndianBinaryReader _reader;
    BigEndianBinaryWriter? _writer;

    public CdpMessage(CommonHeader header)
    {
        Header = header;
        _stream = new(header.FragmentCount * Constants.DefaultMessageFragmentSize);
        _reader = new(_stream);
        _writer = new(_stream);
    }

    public CdpMessage(CommonHeader header, byte[] payload)
    {
        Header = header;
        _stream = new(payload);
        _reader = new(_stream);
        _receivedFragmentCount = header.FragmentCount;
    }

    public CommonHeader Header { get; }

    public uint Id
        => Header.SequenceNumber;

    #region Fragments
    public bool IsComplete
        => _receivedFragmentCount >= Header.FragmentCount;

    ushort _receivedFragmentCount = 0;
    public void AddFragment(byte[] fragment)
    {
        if (_writer == null)
            throw new InvalidOperationException("No fragments expected");

        if (IsComplete)
            throw new InvalidOperationException("Already received all fragments");

        _writer.Write(fragment);
        _receivedFragmentCount++;
    }
    #endregion

    public BinaryReader Read()
    {
        if (!IsComplete)
            throw new InvalidOperationException("Wait for completion");

        _stream.Position = 0;
        return _reader;
    }

    public void Dispose()
    {
        _stream.Dispose();
        _reader.Dispose();
        _writer?.Dispose();
    }
}
