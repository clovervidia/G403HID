using HidSharp;

namespace G403HID
{
    public static class HID
    {
        public static IEnumerable<Device> FindDevices()
        {
            Dictionary<string, (HidDevice?, HidDevice?)> devices = new();

            List<int> compatibleProductIDs = new()
            {
                0xC080, // G303 Daedalus Apex
                0xC083, // G403 Prodigy
            };

            foreach (var item in DeviceList.Local.GetHidDevices())
            {
                if (item.VendorID != 0x046D || !compatibleProductIDs.Contains(item.ProductID))
                {
                    continue;
                }

                var serialNumber = item.GetSerialNumber();

                if (!devices.TryGetValue(serialNumber, out _))
                {
                    devices[serialNumber] = (null, null);
                }

                try
                {
                    foreach (var report in item.GetReportDescriptor().Reports)
                    {
                        if (report.ReportID == 16)
                        {
                            devices[serialNumber] = (item, devices[serialNumber].Item2);
                        }
                        else if (report.ReportID == 17)
                        {
                            devices[serialNumber] = (devices[serialNumber].Item1, item);
                        }
                    }
                }
                catch (Exception)
                {
                    // Exceptions will usually come from not being able to read the report descriptor and can as such be ignored.
                }
            }

            return devices.Where(d => d.Value.Item1 != null && d.Value.Item2 != null).Select(d => new Device(d.Value.Item1!, d.Value.Item2!, d.Key));
        }
    }
}
