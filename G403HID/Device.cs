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

            GetProfileDetails();
            GetFeatureTable();

            Profiles = ReadProfiles();
            OriginalProfiles = ReadProfiles();
            SerialNumber = serialNumber;
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

            var shortEndpointStream = ShortEndpoint.Open();
            var longEndpointStream = LongEndpoint.Open();

            var featureIDBytes = BitConverter.GetBytes(featureID);

            shortEndpointStream.Write(new byte[] { 0x10, 0xFF, 0x00, 0x0F, featureIDBytes[1], featureIDBytes[0], 0x00 });
            var response = longEndpointStream.Read();

            shortEndpointStream.Close();
            longEndpointStream.Close();

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

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, 0x00, 0x1F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                shortEndpointStream.Close();
                longEndpointStream.Close();

                protocolVersion = (response[4], response[5]);

                return (response[4], response[5]);
            }
        }

        // Feature Set (1)
        public List<Feature> FeatureTable = new();
        private List<Feature> GetFeatureTable()
        {
            if (FeatureTable.Count != 0)
            {
                return FeatureTable;
            }

            var featureTable = new List<Feature>();

            var shortEndpointStream = ShortEndpoint.Open();
            var longEndpointStream = LongEndpoint.Open();

            shortEndpointStream.Write(new byte[] { 0x10, 0xFF, 0x01, 0x0F, 0x00, 0x00, 0x00 });
            var response = longEndpointStream.Read();

            // The feature count doesn't include the root feature, so be sure to read one index further to get the last feature.
            var featureCount = response[4];

            for (byte featureIndex = 0; featureIndex < featureCount + 1; featureIndex++)
            {
                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, 0x01, 0x1F, featureIndex, 0x00, 0x00 });
                response = longEndpointStream.Read();
                var featureIDBytes = response.Skip(4).Take(2).Reverse().ToArray();
                featureTable.Add((Feature)BitConverter.ToUInt16(featureIDBytes));
            }

            shortEndpointStream.Close();
            longEndpointStream.Close();

            FeatureTable = featureTable;

            return featureTable;
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

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, deviceNameTypeIndex, 0x0F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                var deviceNameLength = response[4];

                while (deviceName.Length <= deviceNameLength)
                {
                    shortEndpointStream.Write(new byte[] { 0x10, 0xFF, deviceNameTypeIndex, 0x1F, (byte)deviceName.Length, 0x00, 0x00 });
                    response = longEndpointStream.Read();
                    deviceName += Encoding.ASCII.GetString(response, 4, 16);
                }

                shortEndpointStream.Close();
                longEndpointStream.Close();

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

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, adjustableDPIIndex, 0x2F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                shortEndpointStream.Close();
                longEndpointStream.Close();

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

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, reportRateIndex, 0x0F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                shortEndpointStream.Close();
                longEndpointStream.Close();

                return response[4];
            }
        }

        public byte ReportRate
        {
            get
            {
                var reportRateIndex = GetFeatureIndex(Feature.ReportRate);

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, reportRateIndex, 0x1F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                shortEndpointStream.Close();
                longEndpointStream.Close();

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

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, colorLEDEffectsIndex, 0x0F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                shortEndpointStream.Close();
                longEndpointStream.Close();

                ledCount = response[4];

                return response[4];
            }
        }

        // Onboard Profiles (0x8100)
        public byte ProfileCount = 0xFF;
        public byte ButtonCount = 0xFF;
        private void GetProfileDetails()
        {
            var onboardProfileIndex = GetFeatureIndex(Feature.OnboardProfiles);

            var shortEndpointStream = ShortEndpoint.Open();
            var longEndpointStream = LongEndpoint.Open();

            shortEndpointStream.Write(new byte[] { 0x10, 0xFF, onboardProfileIndex, 0x0F, 0x00, 0x00, 0x00 });
            var response = longEndpointStream.Read();

            shortEndpointStream.Close();
            longEndpointStream.Close();

            ProfileCount = response[7];
            ButtonCount = response[9];
        }

        public byte CurrentProfileIndex
        {
            get
            {
                var onboardProfileIndex = GetFeatureIndex(Feature.OnboardProfiles);

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, onboardProfileIndex, 0x4F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                shortEndpointStream.Close();
                longEndpointStream.Close();

                return response[5];
            }
            set
            {
                if (value == 0 || value > ProfileCount)
                {
                    return;
                }

                var onboardProfileIndex = GetFeatureIndex(Feature.OnboardProfiles);

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, onboardProfileIndex, 0x3F, 0x00, value, 0x00 });

                shortEndpointStream.Close();
                longEndpointStream.Close();
            }
        }

        public byte OnboardMode
        {
            get
            {
                var onboardProfileIndex = GetFeatureIndex(Feature.OnboardProfiles);

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                // Can be set with function 0x10
                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, onboardProfileIndex, 0x2F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                shortEndpointStream.Close();
                longEndpointStream.Close();

                return response[4];
            }
        }

        public List<byte> ReadMemoryPage(byte page)
        {
            var onboardProfileIndex = GetFeatureIndex(Feature.OnboardProfiles);

            var memoryBytes = new List<byte>();

            var shortEndpointStream = ShortEndpoint.Open();
            var longEndpointStream = LongEndpoint.Open();

            var readMemoryCommand = new byte[] { 0x11, 0xFF, onboardProfileIndex, 0x5F, 0x00, page, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            for (int address = 0; address < 0x10; address++)
            {
                readMemoryCommand[7] = (byte)(address * 16);

                longEndpointStream.Write(readMemoryCommand);
                memoryBytes.AddRange(longEndpointStream.Read().Skip(4));
            }

            shortEndpointStream.Close();
            longEndpointStream.Close();

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

            var shortEndpointStream = ShortEndpoint.Open();
            var longEndpointStream = LongEndpoint.Open();

            var dataWithHeaders = new List<byte>() { 0x11, 0xFF, onboardProfileIndex, 0x6F, 0x00, page, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            if (data.Count is not 254 and not 256)
            {
                throw new ArgumentException("Data to be written to memory must be 254 or 256 bytes.");
            }

            if (data.Count == 256)
            {
                data.RemoveRange(254, 2);
            }

            data.AddRange(BitConverter.GetBytes(NullFX.CRC.Crc16.ComputeChecksum(NullFX.CRC.Crc16Algorithm.CcittInitialValue0xFFFF, data.ToArray())).Reverse());

            foreach (var item in data.Chunk(16))
            {
                dataWithHeaders.AddRange(new byte[] { 0x11, 0xFF, onboardProfileIndex, 0x7F });
                dataWithHeaders.AddRange(item);
            }

            foreach (var message in dataWithHeaders.Chunk(20))
            {
                longEndpointStream.Write(message);
            }

            shortEndpointStream.Write(new byte[] { 0x10, 0xFF, onboardProfileIndex, 0x8F, 0x00, 0x00, 0x00 });

            shortEndpointStream.Close();
            longEndpointStream.Close();
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
