using HidSharp;
using System.Text;

namespace G403HID
{
    public class Device
    {
        public HidDevice ShortEndpoint;
        public HidDevice LongEndpoint;

        public List<Profile> Profiles;
        private List<Profile> OriginalProfiles;

        public string SerialNumber = string.Empty;

        public enum Feature
        {
            Root = 0,
            FeatureSet = 1,
            FirmwareInfo = 3,
            DeviceNameType = 5,
            DFUControlUnsigned = 0xC1,
            DFUControlSigned = 0xC2,
            DeviceReset = 0x1802,
            AdjustableDPI = 0x2201,
            AngleSnapping = 0x2230,
            SurfaceTuning = 0x2240,
            ReportRate = 0x8060,
            ColorLEDEffects = 0x8070,
            OnboardProfiles = 0x8100,
            MouseButtonSpy = 0x8110,
        }

        public Device(HidDevice shortEndpoint, HidDevice longEndpoint, string serialNumber)
        {
            ShortEndpoint = shortEndpoint;
            LongEndpoint = longEndpoint;

            Profiles = ReadProfiles();
            OriginalProfiles = ReadProfiles();
            SerialNumber = serialNumber;
        }

        public byte[] SendCommand(byte reportID, byte deviceID, byte featureIndex, byte function, List<byte> data)
        {
            if (reportID is not 0x10 and not 0x11)
            {
                throw new ArgumentException("Report ID must be 0x10 or 0x11.");
            }

            if ((reportID == 0x10 && data.Count != 3) || (reportID == 0x11 && data.Count != 16))
            {
                throw new ArgumentException("Data must be 3 bytes for report ID 0x10 or 16 bytes for report ID 0x11.");
            }

            var shortEndpointStream = ShortEndpoint.Open();
            var longEndpointStream = LongEndpoint.Open();

            List<byte> command = new() { reportID, deviceID, featureIndex, function };
            command.AddRange(data);

            switch (reportID)
            {
                case 0x10:
                    shortEndpointStream.Write(command.ToArray());
                    break;
                case 0x11:
                    longEndpointStream.Write(command.ToArray());
                    break;
            }

            var response = longEndpointStream.Read();

            shortEndpointStream.Close();
            longEndpointStream.Close();

            return response;
        }

        // Root Feature (0)
        public byte GetFeatureIndex(int featureID)
        {
            if (FeatureTable.Count != 0)
            {
                var index = FeatureTable.IndexOf((Feature)featureID);

                if (index == -1)
                {
                    throw new NotSupportedException($"Device does not support feature {(Feature)featureID} ({featureID:X4})");
                }

                return (byte)index;
            }

            var featureIDBytes = BitConverter.GetBytes(featureID);

            var response = SendCommand(0x10, 0xFF, 0x00, 0x0F, new() { featureIDBytes[1], featureIDBytes[0], 0x00 });

            return response[4];
        }

        public byte GetFeatureIndex(Feature feature)
        {
            return GetFeatureIndex((int)feature);
        }

        private (byte, byte) protocolVersion = (0xFF, 0xFF);
        public (byte, byte) ProtocolVersion
        {
            get
            {
                if (protocolVersion.Item1 != 0xFF)
                {
                    return protocolVersion;
                }

                var response = SendCommand(0x10, 0xFF, 0x00, 0x1F, new() { 0x00, 0x00, 0x00 });

                protocolVersion = (response[4], response[5]);

                return (response[4], response[5]);
            }
        }

        // Feature Set (1)
        private List<Feature> featureTable = new();
        public List<Feature> FeatureTable
        {
            get
            {
                if (this.featureTable.Count != 0)
                {
                    return this.featureTable;
                }

                var featureTable = new List<Feature>();

                var response = SendCommand(0x10, 0xFF, 0x01, 0x0F, new() { 0x00, 0x00, 0x00 });

                // The feature count doesn't include the root feature, so be sure to read one index further to get the last feature.
                var featureCount = response[4];

                for (byte featureIndex = 0; featureIndex < featureCount + 1; featureIndex++)
                {
                    response = SendCommand(0x10, 0xFF, 0x01, 0x1F, new() { featureIndex, 0x00, 0x00 });
                    var featureIDBytes = response.Skip(4).Take(2).Reverse().ToArray();
                    featureTable.Add((Feature)BitConverter.ToUInt16(featureIDBytes));
                }

                this.featureTable = featureTable;

                return featureTable;
            }
        }

        // Device Name/Type (5)
        private string deviceName = string.Empty;
        public string DeviceName
        {
            get
            {
                if (this.deviceName != string.Empty)
                {
                    return this.deviceName;
                }

                var deviceNameTypeIndex = GetFeatureIndex(Feature.DeviceNameType);

                string deviceName = string.Empty;

                var response = SendCommand(0x10, 0xFF, deviceNameTypeIndex, 0x0F, new() { 0x00, 0x00, 0x00 });
                var deviceNameLength = response[4];

                while (deviceName.Length <= deviceNameLength)
                {
                    response = SendCommand(0x10, 0xFF, deviceNameTypeIndex, 0x1F, new() { (byte)deviceName.Length, 0x00, 0x00 });
                    deviceName += Encoding.ASCII.GetString(response, 4, 16);
                }

                // Trim any trailing null bytes.
                deviceName = deviceName[0..deviceNameLength];

                this.deviceName = deviceName;

                return deviceName;
            }
        }

        // Adjustable DPI (0x2201)
        public short CurrentDPI
        {
            get
            {
                var adjustableDPIIndex = GetFeatureIndex(Feature.AdjustableDPI);

                var response = SendCommand(0x10, 0xFF, adjustableDPIIndex, 0x2F, new() { 0x00, 0x00, 0x00 });

                var currentDPI = BitConverter.ToInt16(response[5..7].Reverse().ToArray());

                return currentDPI;
            }
        }

        // Report Rate (0x8060)
        public byte ReportRateList
        {
            get
            {
                var reportRateIndex = GetFeatureIndex(Feature.ReportRate);

                var response = SendCommand(0x10, 0xFF, reportRateIndex, 0x0F, new() { 0x00, 0x00, 0x00 });

                return response[4];
            }
        }

        public byte ReportRate
        {
            get
            {
                var reportRateIndex = GetFeatureIndex(Feature.ReportRate);

                var response = SendCommand(0x10, 0xFF, reportRateIndex, 0x1F, new() { 0x00, 0x00, 0x00 });

                return response[4];
            }
        }

        // Color LED Effects (0x8070)
        public byte ledCount = 0xFF;
        public byte LEDCount
        {
            get
            {
                if (ledCount != 0xFF)
                {
                    return ledCount;
                }

                var colorLEDEffectsIndex = GetFeatureIndex(Feature.ColorLEDEffects);

                var response = SendCommand(0x10, 0xFF, colorLEDEffectsIndex, 0x0F, new() { 0x00, 0x00, 0x00 });

                ledCount = response[4];

                return response[4];
            }
        }

        // Onboard Profiles (0x8100)
        private byte profileCount = 0xFF;
        public byte ProfileCount
        {
            get
            {
                if (profileCount != 0xFF)
                {
                    return profileCount;
                }

                var onboardProfileIndex = GetFeatureIndex(Feature.OnboardProfiles);

                var response = SendCommand(0x10, 0xFF, onboardProfileIndex, 0x0F, new() { 0x00, 0x00, 0x00 });

                profileCount = response[7];
                buttonCount = response[9];

                return response[7];
            }
        }

        private byte buttonCount = 0xFF;
        public byte ButtonCount
        {
            get
            {
                if (buttonCount != 0xFF)
                {
                    return buttonCount;
                }

                var onboardProfileIndex = GetFeatureIndex(Feature.OnboardProfiles);

                var response = SendCommand(0x10, 0xFF, onboardProfileIndex, 0x0F, new() { 0x00, 0x00, 0x00 });

                profileCount = response[7];
                buttonCount = response[9];

                return response[9];
            }
        }

        public byte CurrentProfileIndex
        {
            get
            {
                var onboardProfileIndex = GetFeatureIndex(Feature.OnboardProfiles);

                var response = SendCommand(0x10, 0xFf, onboardProfileIndex, 0x4F, new() { 0x00, 0x00, 0x00 });

                return response[5];
            }
            set
            {
                if (value == 0 || value > ProfileCount)
                {
                    return;
                }

                var onboardProfileIndex = GetFeatureIndex(Feature.OnboardProfiles);

                SendCommand(0x10, 0xFF, onboardProfileIndex, 0x3F, new() { 0x00, value, 0x00 });
            }
        }

        public byte OnboardMode
        {
            get
            {
                var onboardProfileIndex = GetFeatureIndex(Feature.OnboardProfiles);

                // Can be set with function 0x10
                var response = SendCommand(0x10, 0xFF, onboardProfileIndex, 0x2F, new() { 0x00, 0x00, 0x00 });

                return response[4];
            }
        }

        public List<byte> ReadMemoryPage(byte page)
        {
            var onboardProfileIndex = GetFeatureIndex(Feature.OnboardProfiles);

            var memoryBytes = new List<byte>();

            var readMemoryCommand = new List<byte> { 0x00, page, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            for (int address = 0; address < 0x10; address++)
            {
                readMemoryCommand[3] = (byte)(address * 16);

                var response = SendCommand(0x11, 0xFF, onboardProfileIndex, 0x5F, readMemoryCommand);
                memoryBytes.AddRange(response.Skip(4));
            }

            return memoryBytes;
        }

        public List<Profile> ReadProfiles()
        {
            var profiles = new List<Profile>();

            for (byte profile = 1; profile <= ProfileCount; profile++)
            {
                profiles.Add(Profile.FromBytes(ReadMemoryPage(profile), ButtonCount));
            }

            return profiles;
        }

        public void WriteMemoryPage(byte page, List<byte> data)
        {
            var onboardProfileIndex = GetFeatureIndex(Feature.OnboardProfiles);

            SendCommand(0x11, 0xFF, onboardProfileIndex, 0x6F, new() { 0x00, page, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

            if (data.Count is not 254 and not 256)
            {
                throw new ArgumentException("Data to be written to memory must be 254 or 256 bytes.");
            }

            if (data.Count == 256)
            {
                data.RemoveRange(254, 2);
            }

            data.AddRange(BitConverter.GetBytes(NullFX.CRC.Crc16.ComputeChecksum(NullFX.CRC.Crc16Algorithm.CcittInitialValue0xFFFF, data.ToArray())).Reverse());

            foreach (var message in data.Chunk(16))
            {
                SendCommand(0x11, 0xFF, onboardProfileIndex, 0x7F, message.ToList());
            }

            SendCommand(0x10, 0xFF, onboardProfileIndex, 0x8F, new() { 0x00, 0x00, 0x00 });
        }

        public void WriteProfiles()
        {
            var profileBytes = Profiles.Select(p => p.ToBytes()).ToList();
            var originalProfileBytes = OriginalProfiles.Select(p => p.ToBytes()).ToList();

            // This is my implementation of OMM's "dirty" system. It will only update profiles that have been modified.
            for (byte profile = 1; profile <= ProfileCount; profile++)
            {
                if (profileBytes[profile - 1].SequenceEqual(originalProfileBytes[profile - 1]))
                {
                    continue;
                }

                WriteMemoryPage(profile, profileBytes[profile - 1]);
            }

            Profiles = ReadProfiles();
            OriginalProfiles = ReadProfiles();
        }

        public bool GetProfileVisibility(byte profileIndex)
        {
            if (profileIndex == 0 || profileIndex > ProfileCount)
            {
                return false;
            }

            var firstPage = ReadMemoryPage(0);

            var offset = (profileIndex - 1) * 4 + 2;
            return firstPage[offset] == 1;
        }

        public void SetProfileVisibility(byte profileIndex, bool visible)
        {
            if (profileIndex == 0 || profileIndex > ProfileCount)
            {
                return;
            }

            var firstPage = ReadMemoryPage(0);

            var offset = (profileIndex - 1) * 4 + 2;
            firstPage[offset] = (byte)(visible ? 1 : 0);
            WriteMemoryPage(0, firstPage);
        }
    }
}
