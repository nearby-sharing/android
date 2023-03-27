# Logging (Windows)

Service: `CDPSvc`   
Settings directory: `%LOCALAPPDATA%\ConnectedDevicesPlatform\`    
Settings directory: `C:\Windows\ServiceProfiles\LocalService\AppData\Local\ConnectedDevicesPlatform`    

## Enable Logging
 1. Create `.\CDPGlobalSettings.cdp.override` with this content:
```json
{
   "TraceLog.EnabledHandlerTypes" : 255,
   "TraceLog.Level" : 100
}
```
 2. Run in powershell
```powershell
Get-Service *cdp* | Restart-Service
```
 3. See output at `.\CDPTraces.log`   

## Trace via pipe
```
\\\\.\\pipe\\CDPInOut
```
