using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

public sealed class CdpMessage
{
    readonly EndianBuffer _buffer;

    public CdpMessage(CommonHeader header)
    {
        Header = header;
        _buffer = new(header.FragmentCount * MessageFragmenter.DefaultMessageFragmentSize);
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

    public void Read(out EndianReader reader)
    {
        ThrowIfNotCompleted();

        reader = new(Endianness.BigEndian, _buffer.AsSpan());
    }

    public void ReadBinary(out EndianReader reader, out BinaryMsgHeader header)
    {
        ThrowIfNotCompleted();

        Read(out reader);
        header = BinaryMsgHeader.Parse(ref reader);
    }

    public void ReadBinary(out ValueSet payload, out BinaryMsgHeader header)
    {
        ThrowIfNotCompleted();

        ReadBinary(out EndianReader reader, out header);
        payload = ValueSet.Parse(ref reader);
    }

    void ThrowIfNotCompleted()
    {
        if (!IsComplete)
            throw new InvalidOperationException("Wait for completion");
    }
}
