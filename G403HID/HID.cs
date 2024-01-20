using HidSharp;

namespace G403HID
{
    public static class HID
    {
        public static Device FindDevice()
        {
            HidDevice? shortEndpoint = null;
            HidDevice? longEndpoint = null;

            foreach (var item in DeviceList.Local.GetHidDevices(0x046D, 0xC083))
            {
                try
                {
                    foreach (var report in item.GetReportDescriptor().Reports)
                    {
                        if (report.ReportID == 16)
                        {
                            shortEndpoint = item;
                        }
                        else if (report.ReportID == 17)
                        {
                            longEndpoint = item;
                        }
                    }
                }
                catch (Exception)
                {
                    // Exceptions will usually come from not being able to read the report descriptor and can as such be ignored.
                }

                if (shortEndpoint != null && longEndpoint != null)
                {
                    return new Device(shortEndpoint, longEndpoint);
                }
            }

            throw new ApplicationException("Could not find device.");
        }
    }
}
