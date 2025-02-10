using ShortDev.IO.ValueStream;
using ShortDev.Microsoft.ConnectedDevices.Messages.Session;
using ShortDev.Microsoft.ConnectedDevices.Serialization;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

public sealed class CdpMessage(CommonHeader header)
{
    readonly HeapOutputStream _buffer = new()
    {
        Writer = new()
        {
            Capacity = header.FragmentCount * MessageFragmenter.DefaultMessageFragmentSize
        }
    };

    public CommonHeader Header { get; } = header;

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

    public void Read(out HeapEndianReader reader)
    {
        ThrowIfNotCompleted();

        reader = EndianReader.FromMemory(Endianness.BigEndian, _buffer.WrittenMemory);
    }

    public void ReadBinary(out HeapEndianReader reader, out BinaryMsgHeader header)
    {
        ThrowIfNotCompleted();

        Read(out reader);
        header = BinaryMsgHeader.Parse(ref reader);
    }

    public void ReadBinary(out ValueSet payload, out BinaryMsgHeader header)
    {
        ThrowIfNotCompleted();

        ReadBinary(out HeapEndianReader reader, out header);
        payload = ValueSet.Parse(ref reader);
    }

    void ThrowIfNotCompleted()
    {
        if (!IsComplete)
            throw new InvalidOperationException("Wait for completion");
    }
}
