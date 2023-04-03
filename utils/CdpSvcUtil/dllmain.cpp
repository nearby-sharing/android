// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <combaseapi.h>
#include <string>
#include "Utils.h"
#include "CdpTypes.h"

void PauseIfRequested() {
	if (GetEnvironmentVariableW(L"CDP_WaitForDebugger", NULL, 0) == 0 && GetLastError() == ERROR_ENVVAR_NOT_FOUND)
		return;
	
	WaitForDebugger();
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
	if (IsCdpSvc())
		PauseIfRequested();

	return TRUE;
}

HMODULE CdpSvcLib = 0;
void EnsureCdpSvcLib() {
	if (CdpSvcLib == 0)
		CdpSvcLib = LoadLibraryW(L"C:\\Windows\\System32\\CDPSvc.dll");
}

WinAPIExport void ServiceMain(void* a1, void** a2) {
	PauseIfRequested();
	EnsureCdpSvcLib();

	auto pProc = GetProcAddress(CdpSvcLib, "ServiceMain");
	((void(*)(void*, void**))pProc)(a1, a2);
}

WinAPIExport int SvchostPushServiceGlobals(void* a1) {
	PauseIfRequested();
	EnsureCdpSvcLib();

	auto pProc = GetProcAddress(CdpSvcLib, "SvchostPushServiceGlobals");
	return ((int(*)(void*))pProc)(a1);
}

typedef void* GetInstanceProc(void** pResult);
WinAPIExport void* CdpGetInstance(GetInstanceProc* getInstance) {
	void* pResult = 0;
	getInstance(&pResult);
	return pResult;
}

WinAPIExport HRESULT SetTransportEnabled(SharedSettingsManager* settingsManager, TransportSettingsType transportType, bool enabled) {
	PrintPtr(L"GetGlobalSettings: ", settingsManager->vtbl->GetGlobalSettings);

	SharedGlobalSettings* globalSettings;
	settingsManager->vtbl->GetGlobalSettings(settingsManager, &globalSettings);

	PrintPtr(L"SetTransportEnabled: ", globalSettings->vtbl->SetTransportEnabled);
	return globalSettings->vtbl->SetTransportEnabled(globalSettings, transportType, enabled);
}

WinAPIExport bool GetTransportEnabled(SharedSettingsManager* settingsManager, TransportSettingsType transportType) {
	PrintPtr(L"GetGlobalSettings: ", settingsManager->vtbl->GetGlobalSettings);

	SharedGlobalSettings* globalSettings;
	settingsManager->vtbl->GetGlobalSettings(settingsManager, &globalSettings);

	PrintPtr(L"GetTransportEnabled: ", globalSettings->vtbl->GetTransportEnabled);
	return globalSettings->vtbl->GetTransportEnabled(globalSettings, transportType);
}

WinAPIExport HRESULT CdpInitialize(GetInstanceProc* pGetInstanceSettingsManager) {
	auto settingsManager = (SharedSettingsManager*)CdpGetInstance(pGetInstanceSettingsManager);

	SharedGlobalSettings* globalSettings;
	settingsManager->vtbl->GetGlobalSettings(settingsManager, &globalSettings);

	globalSettings->vtbl->SetTransportEnabled(globalSettings, TransportSettingsType::BLeGatt, true);
	globalSettings->vtbl->SetTransportHostingAllowed(globalSettings, TransportSettingsType::BLeGatt, true);

	return S_OK;
}