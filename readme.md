# G403HID

This is a basic C# implementation of some of the functionality offered by Logitech's HID++ protocol. I wrote this to interface with the G403 Prodigy specifically, but it will likely work with other G mice after some testing.

I was rather curious about how Logitech's OMM (Onboard Memory Manager) could talk to my G403 and adjust things like its RGB color and DPI without needing to install any Windows drivers. The answer was HID. By using HID, OMM can talk to endpoints on my G403 without needing any drivers. Within HID, Logitech created their own protocol called HID++, and that's how you can access the different features of your Logitech keyboards and mice.

My aim was to replicate the functionality offered by OMM and make it available as a library of sorts. I've also implemented some of the other common HID++ features as well, which I'll get into later.

## Compatibility

I've tested this project with a G403 Prodigy and a G303 Daedalus Apex, and it works fine with both of them. Most of the functionality of this project should work with other Logitech G mice that support a similar set of features.

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
  - Get all supported feature IDs in the feature table
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
  - Set the current profile index
  - Mark a profile as shown or hidden from profile cycling
  - Get the onboard mode
  - Read from a memory page
  - Write to a memory page
  - Get the current profiles
  - Write new profiles

All of the fields of the onboard profiles are available to modify, including the the report rate, DPI, profile name, button mappings, and RGB settings.

## Basic Usage

You can find a demo project that runs through the basic commands in `G403HIDDemo`.

To get started, you'll want to locate the device. You can search for all compatible devices connected to your system like so:

```cs
var devices = HID.FindDevices();
```

That will return an `IEnumerable<Device>` that you can loop through to search for your particular device. You can use the `DeviceName` and `SerialNumber` properties to help you narrow down the devices. Note that the serial number reported over USB may not match the serial number on your device's label or cord.

```cs
foreach (var device in devices)
{
	if (device.DeviceName != "your device name" || device.SerialNumber != "your serial number")
	{
		continue;
	}
}
```

## Advanced Features

All of the functionality offered by OMM is handled through the Onboard Profiles feature, and that can be accessed through the `Profiles` property of a `Device`. This is a `List` of `Profile` objects, so you'll need to locate the profile that you wish to edit by index.

To get the current profile index that's being used by the device, check `CurrentProfileIndex`, which returns a one-indexed value for the profile. This means the first profile returns `1`, the second profile `2`, and so on. However, keep in mind that `Profiles` is zero-indexed, so the first profile is at index `0`.

```cs
Console.WriteLine(device.CurrentProfileIndex);
```

You can also write to this property to change the active profile, like so:

```cs
device.CurrentProfileIndex = 2;
```

For devices with multiple profiles, you can map a button to cycle through the different profiles, and one of the things OMM lets you do is hide a profile if you don't want it to come up as you're cycling. This can be useful if your device has three onboard profiles, but you only want to cycle between two of them. Here's how you can check a profile's visibility and update it:

```cs
Console.WriteLine(device.GetProfileVisibility(1));
device.SetProfileVisibility(1, false);
```

Here's how you can read the current RGB color for the first RGB zone on the first profile, which usually corresponds to the mouse wheel or side lighting:

```cs
Console.WriteLine(device.Profiles[0].RGBZone1.RGBColor);
```

And here's how you can set it to red:

```cs
device.Profiles[0].RGBZone1.RGBColor = new(0xFF, 0, 0);
```

You can also change the RGB mode, or the color animation that the zone uses. Besides the RGB lighting being set to a static color or turned off entirely, you can also have it set to breathing, which will make it fade it in and out, or to cycling, which will have it cycle through all of the supported colors.

```cs
device.Profiles[0].RGBZone1.Mode = RGBZone.RGBMode.Breathing;
```

All of the buttons on the mouse can be remapped in one of two ways. You can either have them set to a simple function, like swapping left and right mouse buttons, or adjusting the media volume; or you can map them to a modifier plus a keycode, allowing you to have key combos like Ctrl-C and Win-Shift-S available on your mouse.

```cs
device.Profiles[0].MouseButtonMappings[0] = new SimpleMapping(SimpleMapping.MouseButton.RightButton);
device.Profiles[0].MouseButtonMappings[0] = new ModifierKeypressMapping(ModifierKeypressMapping.KeyCode.S, leftShift: true, leftGui: true);
```

In addition, there's a second layer of button mappings available called G Shift. If you map a button to the G Shift function, you can hold it down to temporarily switch to that second layer of button mappings, giving you extra button mappings to work with.

```cs
device.Profiles[0].MouseButtonMappings[0] = new SimpleMapping(SimpleMapping.MouseButton.GShift);
device.Profiles[0].MouseButtonGShiftMappings[0] = new ModifierKeypressMapping(ModifierKeypressMapping.KeyCode.C, leftCtrl: true);
```

Once you've made modifications to any of the profiles, you'll need to write them back to the device, like so:

```cs
device.WriteProfiles();
```

You can send your own HID++ commands to a `Device` with `SendCommand()`. Just pass in the report ID, device ID, feature index, function, and the data bytes to send. It'll return the response from the device. For example, this is how I get the count of entries in the feature table:

```cs
var response = SendCommand(0x10, 0xFF, 0x01, 0x0F, new() { 0x00, 0x00, 0x00 });
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
