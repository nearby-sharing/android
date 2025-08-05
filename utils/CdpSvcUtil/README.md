# CdpSvcUtil

Change dll in registry
```
Computer\HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\CDPSvc\Parameters
```

(Old: `%SystemRoot%\System32\CDPSvc.dll`)

Restart service
```powershell
Get-Service CdpSvc | Restart-Service
```