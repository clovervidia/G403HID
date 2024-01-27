using G403HID;

namespace G403HIDDemo
{
    public class HIDDemo
    {
        static void Main()
        {
            var devices = HID.FindDevices();

            foreach (var device in devices)
            {
                Console.WriteLine($"Name: {device.DeviceName}");
                Console.WriteLine($"Serial Number: {device.SerialNumber}");
                Console.WriteLine($"Protocol: {device.ProtocolVersion}");

                Console.WriteLine("\nSupported Features:");
                foreach ((Device.Feature feature, int index) item in device.FeatureTable.Select((feature, index) => (featureID: feature, index)))
                {
                    Console.WriteLine($"{item.index:X2}: {item.feature:X} ({item.feature})");
                }

                Console.WriteLine($"\nDPI: {device.CurrentDPI}");
                Console.WriteLine($"{device.ButtonCount} buttons");
                Console.WriteLine($"{device.ProfileCount} profile{(device.ProfileCount > 1 ? "s" : "")}");

                var currentProfileIndex = device.CurrentProfileIndex;
                Console.WriteLine($"Current Profile: {currentProfileIndex}");

                Console.WriteLine("\nRegular Button Mapping:");
                foreach ((ButtonMapping button, int index) button in device.Profiles[currentProfileIndex - 1].MouseButtonMappings.Select((button, index) => (button, index)))
                {
                    Console.WriteLine($"{button.index + 1}: {button.button}");
                }

                Console.WriteLine("\nG Shift Button Mapping:");
                foreach ((ButtonMapping button, int index) button in device.Profiles[currentProfileIndex - 1].MouseButtonGShiftMappings.Select((button, index) => (button, index)))
                {
                    Console.WriteLine($"{button.index + 1}: {button.button}");
                }

                Console.WriteLine("===");
            }
        }
    }
}
