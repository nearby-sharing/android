# Contributing

This guide will explain you how you can contribute to this project!

## What to do

- 🐞 [Report bugs / feature requests](https://github.com/ShortDevelopment/Nearby-Sharing-Windows/issues)
- 🌍 [Add Translation](#add-translation)
- 📖 Better documentation (e.g. FAQ)
- 📱 UI / UX improvements (Xamarin c# android)
- 👨‍💻 Improve libCdp (.NET c#)

## Where to start

- Have a look at [good first issue](https://github.com/nearby-sharing/android/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue")

## How to

- If you have any questions, just ask via [Discord](https://discord.gg/ArFA3Nymr2)!

---

## Add Translation

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
