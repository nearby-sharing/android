using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using System.Diagnostics;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;
public static class MessageFragmenter
{
    public const int DefaultMessageFragmentSize = 16384;

    public static void SendMessage(this IFragmentSender sender, CommonHeader header, ReadOnlySpan<byte> payload, CdpCryptor? cryptor = null)
    {
        if (payload.Length <= DefaultMessageFragmentSize)
        {
            sender.SendFragment(header, payload, cryptor);
            return;
        }

        header.FragmentCount = (ushort)(payload.Length / DefaultMessageFragmentSize);

        var leftover = payload.Length % DefaultMessageFragmentSize;
        if (leftover != 0)
            header.FragmentCount++;

        for (ushort fragmentIndex = 0; fragmentIndex < header.FragmentCount; fragmentIndex++)
        {
            int start = fragmentIndex * DefaultMessageFragmentSize;
            int length = Math.Min(payload.Length - start, DefaultMessageFragmentSize);

            header.FragmentIndex = fragmentIndex;
            sender.SendFragment(header, payload.Slice(start, length), cryptor);
        }
    }

    static void SendFragment(this IFragmentSender sender, CommonHeader header, ReadOnlySpan<byte> payload, CdpCryptor? cryptor)
    {
        Debug.Assert(payload.Length <= DefaultMessageFragmentSize);

        if (cryptor != null)
        {
            cryptor.EncryptMessage(sender, header, payload);
        }
        else
        {
            EndianWriter headerWriter = new(Endianness.BigEndian);
            header.SetPayloadLength(payload.Length);
            header.Write(headerWriter);
            sender.SendFragment(headerWriter.Buffer.AsSpan(), payload);
        }
    }
}

public interface IFragmentSender
{
    /// <summary>
    /// Sends a binary fragment.
    /// </summary>
    void SendFragment(ReadOnlySpan<byte> message);

    /// <summary>
    /// Sends a binary fragment.
    /// </summary>
    void SendFragment(ReadOnlySpan<byte> header, ReadOnlySpan<byte> payload)
        => SendFragment([.. header, .. payload]);
}
