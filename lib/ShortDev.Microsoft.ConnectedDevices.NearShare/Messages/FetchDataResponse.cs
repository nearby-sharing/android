using ShortDev.Microsoft.ConnectedDevices.Serialization;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare.Messages;
internal static class FetchDataResponse
{
    public static void Write(EndianWriter writer, uint contentId, ulong start, int length, out Span<byte> blob)
    {
        CompactBinaryBondWriter bondWriter = new(writer.Buffer);

        bondWriter.WriteFieldBegin(Bond.BondDataType.BT_MAP, 1);
        bondWriter.WriteContainerBegin(count: 4, Bond.BondDataType.BT_WSTRING, Bond.BondDataType.BT_STRUCT);

        WritePropertyBegin(ref bondWriter, "ControlMessage", PropertyType.PropertyType_UInt32);
        bondWriter.WriteFieldBegin(Bond.BondDataType.BT_UINT32, 104);
        bondWriter.WriteUInt32((uint)NearShareControlMsgType.FetchDataResponse);
        bondWriter.WriteStructEnd();

        WritePropertyBegin(ref bondWriter, "ContentId", PropertyType.PropertyType_UInt32);
        bondWriter.WriteFieldBegin(Bond.BondDataType.BT_UINT32, 104);
        bondWriter.WriteUInt32(contentId);
        bondWriter.WriteStructEnd();

        WritePropertyBegin(ref bondWriter, "BlobPosition", PropertyType.PropertyType_UInt64);
        bondWriter.WriteFieldBegin(Bond.BondDataType.BT_UINT64, 106);
        bondWriter.WriteUInt64(start);
        bondWriter.WriteStructEnd();

        WritePropertyBegin(ref bondWriter, "DataBlob", PropertyType.PropertyType_UInt8Array);
        bondWriter.WriteFieldBegin(Bond.BondDataType.BT_LIST, 200);
        bondWriter.WriteContainerBegin(length, Bond.BondDataType.BT_UINT8);

        blob = writer.Buffer.GetSpan(length)[..length];
        writer.Buffer.Advance(length);

        bondWriter.WriteStructEnd();

        bondWriter.WriteStructEnd();
    }

    static void WritePropertyBegin(ref CompactBinaryBondWriter writer, string name, PropertyType type)
    {
        writer.WriteWString(name);

        writer.WriteFieldBegin(Bond.BondDataType.BT_INT32, 0);
        writer.WriteInt32((int)type);
    }
}
