#include "pch.h"
#include <Windows.h>

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
	return TRUE;
}

#define VERSION "0.0.0"

#define EXPORT extern "C" __declspec( dllexport )

EXPORT const char plugin_version[] = VERSION;
EXPORT const int plugin_want_major = 4;
EXPORT const int plugin_want_minor = 0;

#pragma comment(lib, "CdpDissector.lib")
#pragma comment(lib, "bcrypt.lib")
#pragma comment(lib, "Runtime.ServerGC.lib")
#pragma comment(lib, "System.Globalization.Native.Aot.lib")
#pragma comment(lib, "bootstrapperdll.lib")
extern "C" {
	void WINAPI plugin_register_impl();
}

EXPORT void plugin_register() {
	plugin_register_impl();
}
