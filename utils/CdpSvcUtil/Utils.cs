using System.Runtime.InteropServices;

namespace CdpSvcUtil;

static class Utils
{
    public static nint EnsureCdpSvcLib()
        => NativeLibrary.Load("C:\\Windows\\System32\\CDPSvc.dll");

    public static void WaitForDebugger()
    {
        if (IsDebuggerPresent())
            return;

        while (!IsDebuggerPresent())
            Thread.Sleep(100);

        OutputDebugString("[0:] Debugger attached!\n");

        DebugBreak();
    }

    public static void PauseIfRequested()
    {
        if (GetEnvironmentVariable("CDP_WaitForDebugger", default) == 0 && (WIN32_ERROR)Marshal.GetLastWin32Error() == WIN32_ERROR.ERROR_ENVVAR_NOT_FOUND)
            return;

        WaitForDebugger();
    }

    public static unsafe void PrintPtr(string prefix, void* ptr)
    {
        OutputDebugString($"{prefix}{(nint)ptr}\n");
    }
}
