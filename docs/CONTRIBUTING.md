# Contributing

This guide will explain you how you can contribute to this project!   
Contributions are welcome!

There're many possible ways to contribute to nearby-sharing!   
Check if you happen to fall into one of the following categories.   
If not: You can always help others with their setup or [translations](http://translate.nearshare.shortdev.de/).

## General
Have a look at open issues
- [good-first-issue](https://github.com/nearby-sharing/android/issues?q=is%3Aissue%20state%3Aopen%20label%3A%22good%20first%20issue%22) to get started
- [help-wanted](https://github.com/nearby-sharing/android/issues?q=is%3Aissue%20state%3Aopen%20label%3A%22help%20wanted%22) for more complex work that may require specific knowledge

Before you start working on sth serious: Discuss with prior contributors in an open issue or on Discord.   
But you may of cause experiment without asking 😊.

If you have any questions, just ask via [Discord](https://discord.gg/ArFA3Nymr2)!

## Android developers
We are using Material You for the android app.   
I'm always glad for UI / Design support.   
Have a look at [`area::UI-UX`](https://github.com/nearby-sharing/android/issues?q=is%3Aissue%20state%3Aopen%20label%3Aarea%3A%3AUI-UX).

The logic is implemented using direct mappings of the android apis into c#.   
If you happen to know both: Amazing!   
If not: It's not that difficult to learn either of them 😉.   
We always need people upgrading to the latest android apis or implementing new platform-specific features.

## Gtk developers
We are working on a new UI app for linux.   
Checkout the [repo](https://github.com/nearby-sharing/linux).

## .NET developers
The core library is written in pure c#, no platform-specific knowledge required!   
Have a look at issues with the label [`area::libCdp`](https://github.com/nearby-sharing/android/issues?q=is%3Aissue%20label%3Aarea%3A%3AlibCdp%20state%3Aopen).

## Add Translation

> [!NOTE]
> We now support [crowdin](http://translate.nearshare.shortdev.de/)!

> [!IMPORTANT]  
> Make sure to choose the right language code!  
> https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry

- Fork repo on GitHub
- Replace all following `de`s with your language code!
- In the `src` directory
  - Copy the `Resources/values/strings.xml` into a new folder like `Resources/values-de/strings.xml` and translate the individual strings.
  - Copy the `Assets/en/*.html` files into a new folder like `Assets/de/` and translate.
- Add yourself to the `CREDITS.md` file!
- Open one PullRequest for all your changes
