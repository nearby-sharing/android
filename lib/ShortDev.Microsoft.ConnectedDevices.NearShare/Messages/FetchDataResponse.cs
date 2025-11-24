using ShortDev.IO.Bond;
using ShortDev.Microsoft.ConnectedDevices.Serialization;

namespace ShortDev.Microsoft.ConnectedDevices.NearShare.Messages;
internal static class FetchDataResponse
{
    public static void Write<TWriter>(ref TWriter writer, uint contentId, ulong start, int length, out Span<byte> blob) where TWriter : struct, IEndianWriter, allows ref struct
    {
        CompactBinaryWriter<TWriter> bondWriter = new(ref writer);

        bondWriter.WriteFieldBegin(BondDataType.BT_MAP, 1);
        bondWriter.WriteContainerBegin(count: 4, BondDataType.BT_WSTRING, BondDataType.BT_STRUCT);

        WritePropertyBegin(ref bondWriter, "ControlMessage", PropertyType.PropertyType_UInt32);
        bondWriter.WriteFieldBegin(BondDataType.BT_UINT32, 104);
        bondWriter.WriteUInt32((uint)NearShareControlMsgType.FetchDataResponse);
        bondWriter.WriteStructEnd();

        WritePropertyBegin(ref bondWriter, "ContentId", PropertyType.PropertyType_UInt32);
        bondWriter.WriteFieldBegin(BondDataType.BT_UINT32, 104);
        bondWriter.WriteUInt32(contentId);
        bondWriter.WriteStructEnd();

        WritePropertyBegin(ref bondWriter, "BlobPosition", PropertyType.PropertyType_UInt64);
        bondWriter.WriteFieldBegin(BondDataType.BT_UINT64, 106);
        bondWriter.WriteUInt64(start);
        bondWriter.WriteStructEnd();

        WritePropertyBegin(ref bondWriter, "DataBlob", PropertyType.PropertyType_UInt8Array);
        bondWriter.WriteFieldBegin(BondDataType.BT_LIST, 200);
        bondWriter.WriteContainerBegin(length, BondDataType.BT_UINT8);

        blob = writer.GetSpan(length)[..length];
        writer.Advance(length);

        bondWriter.WriteStructEnd();

        bondWriter.WriteStructEnd();
    }

    static void WritePropertyBegin<TWriter>(ref CompactBinaryWriter<TWriter> writer, string name, PropertyType type) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.WriteWString(name);

        writer.WriteFieldBegin(BondDataType.BT_INT32, 0);
        writer.WriteInt32((int)type);
    }
}
