# G403HID

This is a basic C# implementation of some of the functionality offered by Logitech's HID++ protocol. I wrote this to interface with the G403 Prodigy specifically, but it will likely work with other G mice after some testing.

I was rather curious about how Logitech's OMM (Onboard Memory Manager) could talk to my G403 and adjust things like its RGB color and DPI without needing to install any Windows drivers. The answer was HID. By using HID, OMM can talk to endpoints on my G403 without needing any drivers. Within HID, Logitech created their own protocol called HID++, and that's how you can access the different features of your Logitech keyboards and mice.

My aim was to replicate the functionality offered by OMM and make it available as a library of sorts. I've also implemented some of the other common HID++ features as well, which I'll get into later.

## Compatibility

As noted earlier, I've only tested this with a G403 Prodigy, as that's the only Logitech G mouse I have available to me, but I believe that most of the functionality of this project will work with other mice that support a similar set of features.

If anyone has another G mouse and wants to test it for compatibility, I'm open to PRs.

My understanding is that most of Logitech's products, not just their gaming peripherals, use HID++ to allow desktop programs to customize them. This is how you can add more devices to a Unifying receiver, for example.

I intend for this project to primarily support Logitech's gaming mice, but if there's interest in supporting other types of devices, like keyboards and the Unifying receiver, I'm open to discussion.

## Features and Functionality

HID++ allows a device to expose certain features to the host computer, allowing it to get and set different parameters. These features can include the DPI, RGB effects, and so on.

These features are assigned IDs that are global to all devices. A given feature ID will always refer to the same feature across all devices. But since different devices may have different sets of features, in order to use a feature on a given device, you first have to query it to see which index in the feature table that feature ID is located at. Once you have the feature index, you can then interface with that feature on the device.

This project implements a subset of the features available on the G403 Prodigy, as I mainly wanted to replicate the options available through OMM. Here are those features and the functionality exposed by them:

- Root Feature (0)
  - Get the feature index of a given feature ID
- Feature Set (1)
  - Get all supported feature IDs
- Device Name/Type (5)
  - Get the device name
- Adjustable DPI (0x2201)
  - Get the current DPI
- Report Rate (0x8060)
  - Get the supported report rate list
  - Get the report rate
- Color LED Effects (0x8070)
  - Get the LED count
- Onboard Profiles (0x8110)
  - Get the profile count
  - Get the button count
  - Get the current profile index
  - Get the onboard mode
  - Get the current config
  - Write a new config

All of the fields of the onboard profiles are available to modify, including the the report rate, DPI, profile name, button mappings, and RGB settings.

## Basic Usage

You can find a demo project that runs through the basic commands in `G403HIDDemo`.

To get started, you'll want to locate the device.

```cs
var device = HID.FindDevice();
```

Once you've found the device, you can start to access the different properties and methods available to it.

For example, to get the device's name:

```cs
Console.WriteLine(device.DeviceName);
```

## Advanced Features

All of the functionality offered by OMM is handled through the Onboard Profiles feature, and that can be accessed through the `Config` property of a `Device`.

For example, here's how you can read the current RGB color for the mouse wheel RGB zone:

```cs
Console.WriteLine(device.Config.WheelRGB.RGBColor);
```

And here's how you can set it to red:

```cs
device.Config.WheelRGB.RGBColor = new(0xFF, 0, 0);
```

You can also change the RGB mode, or the color animation that the zone uses. Besides the RGB lighting being set to a static color or turned off entirely, you can also have it set to breathing, which will make it fade it in and out, or to cycling, which will have it cycle through all of the supported colors.

```cs
device.Config.WheelRGB.Mode = RGBZone.RGBMode.Breathing;
```

All of the buttons on the mouse can be remapped in one of two ways. You can either have them set to a simple function, like swapping left and right mouse buttons, or adjusting the media volume; or you can map them to a modifier plus a keycode, allowing you to have key combos like Ctrl-C and Win-Shift-S available on your mouse.

```cs
device.Config.MouseButtonMappings[0] = new SimpleMapping(SimpleMapping.MouseButton.RightButton);
device.Config.MouseButtonMappings[0] = new ModifierKeypressMapping(ModifierKeypressMapping.KeyCode.S, leftShift: true, leftGui: true);
```

In addition, there's a second layer of button mappings available called G Shift. If you map a button to the G Shift function, you can hold it down to temporarily switch to that second layer of button mappings, giving you five extra button mappings to work with.

```cs
device.Config.MouseButtonMappings[0] = new SimpleMapping(SimpleMapping.MouseButton.GShift);
device.Config.MouseButtonGShiftMappings[0] = new ModifierKeypressMapping(ModifierKeypressMapping.KeyCode.C, leftCtrl: true);
```

Once you've made modifications to the config, you'll need to write it back to the device, like so:

```cs
device.WriteConfig();
```

## Acknowledgements

I referenced a number of open source projects in order to understand how the HID++ protocol works, and without their efforts, it would've taken me much longer to get this project to a working state. I appreciate the work put in by the contributors of the following projects:

- [hidpp](https://github.com/cvuchener/hidpp)
- [Nestor Lopez Casado's HID++ files](https://drive.google.com/folderview?id=0BxbRzx7vEV7eWmgwazJ3NUFfQ28)
- [Peter Wu's HID++ files](https://lekensteyn.nl/files/logitech/)
- [Linux Kernel](https://git.kernel.org/pub/scm/linux/kernel/git/hid/hid.git/tree/drivers/hid/hid-logitech-hidpp.c)
- [Solaar](https://github.com/pwr-Solaar/Solaar)
- [OpenRGB](https://gitlab.com/CalcProgrammer1/OpenRGB)
- [libratbag](https://github.com/libratbag/libratbag)
