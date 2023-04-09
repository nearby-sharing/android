#pragma once

#include <combaseapi.h>
#include <string>

#define WinAPIExport extern "C" __declspec( dllexport )

bool IsCdpSvc() {
	const int length = 35;
	LPWSTR moduleNameBuffer = new WCHAR[length];
	GetModuleFileNameW(NULL, moduleNameBuffer, length);

	if (moduleNameBuffer != L"C:\\WINDOWS\\system32\\svchost.exe")
		return false;

	auto cmd = std::wstring(GetCommandLineW());
	return cmd.find(L"CDPSvc") != std::string::npos;
}

void WaitForDebugger() {
	if (IsDebuggerPresent())
		return;

	while (!IsDebuggerPresent())
		Sleep(100);

	OutputDebugStringW(L"[0:] Debugger attached!\n");

	DebugBreak();
}

void PrintPtr(LPCWSTR prefix, void* ptr) {
	auto str = std::to_wstring((long long)ptr);
	str.insert(0, prefix);
	str.append(L"\n");
	OutputDebugStringW(str.c_str());
}