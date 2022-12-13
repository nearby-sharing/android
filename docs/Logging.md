# Logging

Service: `CDPSvc`
Settings directory: `%LOCALAPPDATA%\ConnectedDevicesPlatform\`    
   
Create `.\CDPGlobalSettings.cdp.override` with this content:
```json
{
   "TraceLog.EnabledHandlerTypes" : 255,
   "TraceLog.Level" : 10
}
```
   
Output: `.\CDPTraces.log`   
   
```
\\\\.\\pipe\\CDPInOut
```