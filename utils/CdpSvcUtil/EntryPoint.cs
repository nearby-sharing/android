using System.Runtime.InteropServices;

namespace CdpSvcUtil;

public static unsafe class EntryPoint
{

    [UnmanagedCallersOnly(EntryPoint = nameof(DllMain))]
    static BOOL DllMain(HMODULE hModule, uint ul_reason_for_call, nint lpReserved)
    {
        Utils.PauseIfRequested();

        return true;
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(ServiceMain))]
    static unsafe void ServiceMain(void* a1, void** a2)
    {
        Utils.PauseIfRequested();

        var pProc = (delegate* unmanaged<void*, void**, void>)NativeLibrary.GetExport(Utils.EnsureCdpSvcLib(), nameof(ServiceMain));
        pProc(a1, a2);
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(SvchostPushServiceGlobals))]
    static unsafe int SvchostPushServiceGlobals(void* a1)
    {
        Utils.PauseIfRequested();

        var pProc = (delegate* unmanaged<void*, int>)NativeLibrary.GetExport(Utils.EnsureCdpSvcLib(), nameof(SvchostPushServiceGlobals));
        var result = pProc(a1);

        // cdp!GetHost will be called by CdpSvc
        // ToDo: custom init here

        return result;
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(CdpGetInstance))]
    static void* CdpGetInstance(delegate* unmanaged<void**, void*> getInstance)
    {
        void* pResult = null;
        getInstance(&pResult);
        return pResult;
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(SetTransportEnabled))]
    static HRESULT SetTransportEnabled(SharedSettingsManager* settingsManager, TransportSettingsType transportType, BOOL enabled)
    {
        Utils.PrintPtr("GetGlobalSettings: ", settingsManager->vtbl->GetGlobalSettings);

        SharedGlobalSettings* globalSettings;
        settingsManager->vtbl->GetGlobalSettings(settingsManager, &globalSettings);

        Utils.PrintPtr("SetTransportEnabled: ", globalSettings->vtbl->SetTransportEnabled);
        return globalSettings->vtbl->SetTransportEnabled(globalSettings, transportType, enabled);
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(GetTransportEnabled))]
    static BOOL GetTransportEnabled(SharedSettingsManager* settingsManager, TransportSettingsType transportType)
    {
        Utils.PrintPtr("GetGlobalSettings: ", settingsManager->vtbl->GetGlobalSettings);

        SharedGlobalSettings* globalSettings;
        settingsManager->vtbl->GetGlobalSettings(settingsManager, &globalSettings);

        Utils.PrintPtr("GetTransportEnabled: ", globalSettings->vtbl->GetTransportEnabled);
        return globalSettings->vtbl->GetTransportEnabled(globalSettings, transportType);
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(CdpInitialize))]
    static HRESULT CdpInitialize(delegate* unmanaged<void**, void*> pGetInstanceSettingsManager)
    {
        SharedSettingsManager* settingsManager;
        pGetInstanceSettingsManager((void**)&settingsManager);

        SharedGlobalSettings* globalSettings;
        settingsManager->vtbl->GetGlobalSettings(settingsManager, &globalSettings);

        globalSettings->vtbl->SetTransportEnabled(globalSettings, TransportSettingsType.BLeGatt, true);
        globalSettings->vtbl->SetTransportHostingAllowed(globalSettings, TransportSettingsType.BLeGatt, true);

        OutputDebugString("Enabled BLeGatt");

        globalSettings->vtbl->SetTransportEnabled(globalSettings, TransportSettingsType.WiFiDirect, true);
        globalSettings->vtbl->SetTransportHostingAllowed(globalSettings, TransportSettingsType.WiFiDirect, true);

        OutputDebugString("Enabled WiFiDirect");

        globalSettings->vtbl->SetTransportEnabled(globalSettings, TransportSettingsType.Udp, true);
        globalSettings->vtbl->SetTransportHostingAllowed(globalSettings, TransportSettingsType.Udp, true);

        OutputDebugString("Enabled Udp");

        return HRESULT.S_OK;
    }
}
