---
title: About
outputFileName: index.html
---

# Nearby Sharing (`NearShare`)

This repo contains a fully functional implementation of the [`MS-CDP`](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-cdp) and `NearShare` protocol that powers the `Windows 10+` built-in sharing functionality (Aka [`Project Rome`](https://github.com/microsoft/project-rome)).

<a href="https://play.google.com/store/apps/details?id=de.shortdev.nearby_sharing_windows" target="_blank" rel="noopener noreferrer"><img src="https://cdn.shortdev.de/badges/google-play.svg?cb=2" width="200"/></a>
<a href="https://fdroid.nearshare.shortdev.de/fdroid/repo/" target="_blank" rel="noopener noreferrer"><img src="https://cdn.shortdev.de/badges/fdroid.svg?cb=2" width="200"/></a>
<a href="https://github.com/nearby-sharing/android/releases/latest" target="_blank" rel="noopener noreferrer"><img src="https://cdn.shortdev.de/badges/github-releases.svg" width="200"/></a>
<a href="https://nearshare.shortdev.de/download/" target="_blank" rel="noopener noreferrer"><img src="https://cdn.shortdev.de/badges/other-platforms.svg" width="200"/></a>

## Building

This project consists of `.NET 9` library and `Android` projects.  
You can use the [`Visual Studio`](https://visualstudio.microsoft.com/de/) solution or the [`dotnet`](https://dotnet.microsoft.com/en-us/download) cli to build and deploy the app:

```shell
cd src
dotnet run
```

## Contribute

Contributions are welcome!  
Have a look at the [contributing guidelines](./docs/CONTRIBUTING.md) to get started!

## License

This project is licensed under [GPL-3.0](LICENSE.md).
