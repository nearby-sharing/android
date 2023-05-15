using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Networking;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static CdpDissector.LibWireShark;

namespace CdpDissector;

public static unsafe partial class Registration
{
    // https://github.com/wireshark/wireshark/blob/master/doc/README.plugins

    #region TryRun
    [DllImport("user32.dll")]
    public static extern int MessageBox(nint hWnd, string lpText, string lpCaption, uint uType);

    static T? TryRun<T>(Func<T> func, [CallerMemberName] string callerName = "")
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            MessageBox(0, ex.ToString(), $"Error in {callerName}", 0);
            return default;
        }
    }
    #endregion

    [UnmanagedCallersOnly(EntryPoint = nameof(plugin_register_impl), CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void plugin_register_impl()
    {
        TryRun(() =>
        {
            var plugin = (ProtoPlugin*)Marshal.AllocHGlobal(sizeof(ProtoPlugin));
            plugin->register = &proto_register_mscdp;
            plugin->handoff = null;

            proto_register_plugin(plugin);

            return 0;
        });
    }

    #region plugin
    [LibraryImport("libwireshark")]
    private static unsafe partial void proto_register_plugin(ProtoPlugin* plugin);

    public struct ProtoPlugin
    {
        public delegate* unmanaged[Cdecl]<void> register;
        public delegate* unmanaged[Cdecl]<void> handoff;
    }
    #endregion

    private static ProtocolHandle hProtocol = ProtocolHandle.Invalid;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void proto_register_mscdp()
    {
        TryRun(() =>
        {
            hProtocol = proto_register_protocol("Microsoft Connected Devices Platform", "MS-CDP", "ms_cdp");
            var hDissector = create_dissector_handle(&dissect_mscdp, hProtocol);
            register_postdissector(hDissector);

            return 0;
        });
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int dissect_mscdp(TvBuff* tvb, PacketInfo* pinfo, ProtoTree* tree, void* data)
    {
        return TryRun(() =>
        {
            EndianReader reader = new(Endianness.BigEndian, new UnsafeStream() { Buffer = tvb });

            if (CommonHeader.TryParse(reader, out var header, out var ex))
                proto_tree_add_protocol_format(tree, hProtocol, tvb, 0, -1, $"Das ist ein Test! {header?.SessionId}");
            else
                proto_tree_add_protocol_format(tree, hProtocol, tvb, 0, -1, ex.Message);

            return tvb_captured_length(tvb);
        });
    }
}