namespace CdpSvcUtil;

#region SharedGlobalSettings

enum TransportSettingsType
{
    Udp = 1,
    Tcp = 2,
    Cloud = 3,
    Rfcomm = 4,
    WiFiDirect = 5,
    BLeGatt = 6
};

unsafe struct SharedGlobalSettings
{
    public SharedGlobalSettingsVtbl* vtbl;
};

unsafe struct SharedGlobalSettingsVtbl
{
    fixed char offset[336];
    public delegate* unmanaged<SharedGlobalSettings*, TransportSettingsType, BOOL> GetTransportEnabled;
	fixed char offset2[968 - 336 - 8];
    public delegate* unmanaged<SharedGlobalSettings*, TransportSettingsType, BOOL, HRESULT> SetTransportEnabled;
	void* SetTcpTransportUpgradeRequired;
    void* SetProtocolVersionBrokerEnabled;
    public delegate* unmanaged<SharedGlobalSettings*, TransportSettingsType, BOOL, HRESULT> SetTransportHostingAllowed;
};

#endregion

#region SharedSettingsManager

unsafe struct SharedSettingsManager
{
    public SharedSettingsManagerVtbl* vtbl;
};

unsafe struct SharedSettingsManagerVtbl
{
    void* abc;
    public delegate* unmanaged<SharedSettingsManager*, void> Terminate;
    public delegate* unmanaged<SharedSettingsManager*, SharedGlobalSettings**, void*> GetGlobalSettings;
    // GetUserSettings
    // ...
};

#endregion
