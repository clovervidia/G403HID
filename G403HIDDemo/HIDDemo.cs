using G403HID;

namespace G403HIDDemo
{
    public class HIDDemo
    {
        static void Main()
        {
            var device = HID.FindDevice();

            Console.WriteLine($"Name: {device.DeviceName}");
            Console.WriteLine($"Protocol: {device.ProtocolVersion}");

            Console.WriteLine("\nSupported Features:");
            foreach ((Device.Feature feature, int index) item in device.FeatureTable.Select((feature, index) => (featureID: feature, index)))
            {
                Console.WriteLine($"{item.index:X2}: {item.feature:X} ({item.feature})");
            }

            Console.WriteLine($"\nDPI: {device.CurrentDPI}");
            Console.WriteLine($"{device.ButtonCount} buttons");
            Console.WriteLine($"{device.ProfileCount} profile{(device.ProfileCount > 1 ? "s" : "")}");

            Console.WriteLine("\nRegular Button Mapping:");
            foreach ((ButtonMapping button, int index) button in device.Config.MouseButtonMappings.Select((button, index) => (button, index)))
            {
                Console.WriteLine($"{button.index + 1}: {button.button}");
            }

            Console.WriteLine("\nG Shift Button Mapping:");
            foreach ((ButtonMapping button, int index) button in device.Config.MouseButtonGShiftMappings.Select((button, index) => (button, index)))
            {
                Console.WriteLine($"{button.index + 1}: {button.button}");
            }
        }
    }
}
