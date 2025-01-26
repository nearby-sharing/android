---
title: About
outputFileName: index.html
---

# Nearby Sharing (`NearShare`)

This repo contains a fully functional implementation of the [`MS-CDP`](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-cdp) and `NearShare` protocol that powers the `Windows 10+` built-in sharing functionality (Aka [`Project Rome`](https://github.com/microsoft/project-rome)).

> [!NOTE]
> This repository contains a fully functional `Android` app that serves as a frontend of `libCdp`.  
> You can visit [nearshare.shortdev.de](https://nearshare.shortdev.de) for download and setup instructions.

> [!TIP]
> For other platforms have a look at the [other repositories](https://github.com/nearby-sharing)!

## Building

This project consists of `.NET 8` library and `Android` projects.  
You can use the [`Visual Studio`](https://visualstudio.microsoft.com/de/) solution or the [`dotnet`](https://dotnet.microsoft.com/en-us/download) cli to build and deploy the app:

```shell
cd src
dotnet run
```

## Contribute

Contributions are welcome!  
Have a look at the [contributing guidelines](CONTRIBUTING.md) to get started!

## License

This project is licensed under [GPL-3.0](LICENSE.md).
