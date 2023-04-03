#pragma once

#include "pch.h"

#pragma region SharedGlobalSettings

enum TransportSettingsType {
	Tcp = 2,
	Rfcomm = 4,
	WiFiDirect = 4,
	BLeGatt = 6
};

struct SharedGlobalSettingsVtbl;

struct SharedGlobalSettings {
	SharedGlobalSettingsVtbl* vtbl;
};

struct SharedGlobalSettingsVtbl {
	char offset[336];
	bool (*GetTransportEnabled)(SharedGlobalSettings* that, TransportSettingsType transportType);
	char offset2[968 - 336 - 8];
	HRESULT(*SetTransportEnabled)(SharedGlobalSettings* that, TransportSettingsType transportType, bool enabled);
	void* SetTcpTransportUpgradeRequired;
	void* SetProtocolVersionBrokerEnabled;
	HRESULT(*SetTransportHostingAllowed)(SharedGlobalSettings* that, TransportSettingsType transportType, bool allowed);
};

#pragma endregion

#pragma region SharedSettingsManager

struct SharedSettingsManagerVtbl;

struct SharedSettingsManager {
	SharedSettingsManagerVtbl* vtbl;
};

struct SharedSettingsManagerVtbl
{
	void* abc;
	void (*Terminate)(SharedSettingsManager* that);
	void* (*GetGlobalSettings)(SharedSettingsManager* that, SharedGlobalSettings** result);
	// GetUserSettings
	// ...
};

#pragma endregion