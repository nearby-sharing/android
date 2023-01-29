# FAQ
## Share to windows

> **Note**
> Follow the new [setup guide](https://nearshare.shortdev.de/docs/setup)!

### What can I share?
You can share **files** and **urls** to any Windows 10 / 11 devices.

### How can I share?
You can use the "Send" button in the app to select a file or simple use the **native sharing menu**.
(e.g. from your gallery or browser)

### Can I send multiple files?
**Yes**, you might need to select multiple files in your gallery app and use the android native sharing menu.   
From there you can select this app.

### What transport technologies are used?
In theory, any type of transport can be used but currently only **Bluetooth** (Rfcomm) and **Wifi** (local network) are supported.   
I'll might make other types available in the future.

### My device is `not supported`
Ensure that you've set your "near share" settings in windows to send and receive to / from `all devices`.

### My transfer is slow
In general bluetooth is slower than wifi, so try to **always choose wifi** if you can.

### My device is only shown with Bluetooth
For the transfer to work via wifi you have to ensure both devices are on the same network.   
You should also make sure that your router allows devices to communicate (might not be the case in a (public) hotspot)!

### My devices is not shown at all
This is a builtin feature of windows.   
Check if "Nearby Sharing" is enabled in the settings of your windows device (<a href="ms-settings:crossdevice">ms-settings:crossdevice</a>).    

## Receive from windows

> **Note**   
> I'm currently working on this feature   
> Track the status in [Issue #8](https://github.com/ShortDevelopment/Nearby-Sharing-Windows/issues/8)

Feedback: https://forms.office.com/r/j2Fp5biXKB

### My PC does not connect
Please ensure that **no** device is connected!
