using System.Runtime.InteropServices;

namespace CdpDissector;

public static unsafe class LibWireShark
{
    private const string LibWireShark_Name = "libwireshark";

    [DllImport(LibWireShark_Name)]
    public static extern ProtocolHandle proto_register_protocol(string name, string shortName, string filterName);

    [DllImport(LibWireShark_Name)]
    public static unsafe extern DissectorHandle create_dissector_handle(delegate* unmanaged[Cdecl]<TvBuff*, PacketInfo*, ProtoTree*, void*, int> callback, ProtocolHandle protocol);

    [DllImport(LibWireShark_Name)]
    public static unsafe extern void register_postdissector(DissectorHandle handle);

    [DllImport(LibWireShark_Name)]
    public static unsafe extern void col_set_str(ColumnInfo* cinfo, ColumnId column, string value);

    [DllImport(LibWireShark_Name)]
    public static unsafe extern void proto_tree_add_protocol_format(ProtoTree* tree, ProtocolHandle hfindex, TvBuff* tvb, int start, int length, string format);

    [DllImport(LibWireShark_Name)]
    public static unsafe extern int tvb_captured_length(TvBuff* tvb);

    [DllImport(LibWireShark_Name)]
    public static unsafe extern byte tvb_get_guint8(TvBuff* tvb, int offset);
}

public readonly struct ProtocolHandle
{
    public readonly int Value;

    private ProtocolHandle(int value)
        => Value = value;

    public static ProtocolHandle Invalid
        => new(-1);
}

public unsafe readonly struct DissectorHandle
{
    public readonly void* Value;
}

public readonly struct ProtoTree { }

/// <summary>
/// <see href="https://github.com/wireshark/wireshark/blob/37dd1d007bbf1f4adba450c5e20c26f6edb5aa56/epan/packet_info.h#L44"/>
/// </summary>
public readonly unsafe struct PacketInfo
{
    /// <summary>
    /// name of protocol currently being dissected
    /// </summary>
    public readonly char* current_proto;
    /// <summary>
    /// Column formatting information
    /// </summary>
    public readonly ColumnInfo* cinfo;
}

public readonly struct ColumnInfo { }

public readonly unsafe struct TvBuff {
    public readonly TvBuff* next;
    readonly void* ops;
    public readonly bool initialized;
    public readonly int flags;
    readonly void* ds_tvb;

}

public enum ColumnId
{
    ABS_YMD_TIME = 0,
    ABS_YDOY_TIME = 1,
    ABS_TIME = 2,
    CUMULATIVE_BYTES = 3,
    CUSTOM = 4,
    DELTA_TIME = 5,
    DELTA_TIME_DIS = 6,
    RES_DST = 7,
    UNRES_DST = 8,
    RES_DST_PORT = 9,
    UNRES_DST_PORT = 10,
    DEF_DST = 11,
    DEF_DST_PORT = 12,
    EXPERT = 13,
    IF_DIR = 14,
    FREQ_CHAN = 15,
    DEF_DL_DST = 16,
    DEF_DL_SRC = 17,
    RES_DL_DST = 18,
    UNRES_DL_DST = 19,
    RES_DL_SRC = 20,
    UNRES_DL_SRC = 21,
    RSSI = 22,
    TX_RATE = 23,
    DSCP_VALUE = 24,
    INFO = 25,
    RES_NET_DST = 26,
    UNRES_NET_DST = 27,
    RES_NET_SRC = 28,
    UNRES_NET_SRC = 29,
    DEF_NET_DST = 30,
    DEF_NET_SRC = 31,
    NUMBER = 32,
    PACKET_LENGTH = 33,
    PROTOCOL = 34,
    REL_TIME = 35,
    DEF_SRC = 36,
    DEF_SRC_PORT = 37,
    RES_SRC = 38,
    UNRES_SRC = 39,
    RES_SRC_PORT = 40,
    UNRES_SRC_PORT = 41,
    UTC_YMD_TIME = 42,
    UTC_YDOY_TIME = 43,
    UTC_TIME = 44,
    CLS_TIME = 45,
}
