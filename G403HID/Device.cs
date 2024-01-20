using HidSharp;
using System.Text;

namespace G403HID
{
    public class Device
    {
        public HidDevice ShortEndpoint;
        public HidDevice LongEndpoint;

        public Config Config;
        public Config OriginalConfig;

        private readonly byte FeatureSetIndex;
        private readonly byte FirmwareInfoIndex;
        private readonly byte DeviceNameTypeIndex;
        private readonly byte AdjustableDPIIndex;
        private readonly byte ReportRateIndex;
        private readonly byte ColorLEDEffectsIndex;
        private readonly byte OnboardProfileIndex;

        public enum Feature
        {
            Root = 0,
            FeatureSet = 1,
            FirmwareInfo = 3,
            DeviceNameType = 5,
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

        public Device(HidDevice shortEndpoint, HidDevice longEndpoint)
        {
            ShortEndpoint = shortEndpoint;
            LongEndpoint = longEndpoint;

            FeatureSetIndex = GetFeature(Feature.FeatureSet);
            FirmwareInfoIndex = GetFeature(Feature.FirmwareInfo);
            DeviceNameTypeIndex = GetFeature(Feature.DeviceNameType);
            OnboardProfileIndex = GetFeature(Feature.OnboardProfiles);
            ReportRateIndex = GetFeature(Feature.ReportRate);
            ColorLEDEffectsIndex = GetFeature(Feature.ColorLEDEffects);
            AdjustableDPIIndex = GetFeature(Feature.AdjustableDPI);

            Config = ReadConfig();
            OriginalConfig = ReadConfig();
        }

        // Root Feature (0)
        public byte GetFeature(int featureID)
        {
            var shortEndpointStream = ShortEndpoint.Open();
            var longEndpointStream = LongEndpoint.Open();

            var featureIDBytes = BitConverter.GetBytes(featureID);

            shortEndpointStream.Write(new byte[] { 0x10, 0xFF, 0x00, 0x0F, featureIDBytes[1], featureIDBytes[0], 0x00 });
            var response = longEndpointStream.Read();

            shortEndpointStream.Close();
            longEndpointStream.Close();

            return response[4];
        }

        public byte GetFeature(Feature feature)
        {
            return GetFeature((int)feature);
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

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, FeatureSetIndex, 0x0F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                // The feature count doesn't include the root feature, so be sure to read one index further to get the last feature.
                var featureCount = response[4];

                for (byte featureIndex = 0; featureIndex < featureCount + 1; featureIndex++)
                {
                    shortEndpointStream.Write(new byte[] { 0x10, 0xFF, FeatureSetIndex, 0x1F, featureIndex, 0x00, 0x00 });
                    response = longEndpointStream.Read();
                    var featureIDBytes = response.Skip(4).Take(2).Reverse().ToArray();
                    featureTable.Add((Feature)BitConverter.ToUInt16(featureIDBytes));
                }

                shortEndpointStream.Close();
                longEndpointStream.Close();

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

                string deviceName = string.Empty;

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, DeviceNameTypeIndex, 0x0F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                var deviceNameLength = response[4];

                while (deviceName.Length <= deviceNameLength)
                {
                    shortEndpointStream.Write(new byte[] { 0x10, 0xFF, DeviceNameTypeIndex, 0x1F, (byte)deviceName.Length, 0x00, 0x00 });
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
                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, AdjustableDPIIndex, 0x2F, 0x00, 0x00, 0x00 });
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
                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, ReportRateIndex, 0x0F, 0x00, 0x00, 0x00 });
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
                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, ReportRateIndex, 0x1F, 0x00, 0x00, 0x00 });
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

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, ColorLEDEffectsIndex, 0x0F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                shortEndpointStream.Close();
                longEndpointStream.Close();

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

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, OnboardProfileIndex, 0x0F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                shortEndpointStream.Close();
                longEndpointStream.Close();

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

                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, OnboardProfileIndex, 0x0F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                shortEndpointStream.Close();
                longEndpointStream.Close();

                profileCount = response[7];
                buttonCount = response[9];

                return response[9];
            }
        }

        public byte CurrentProfileIndex
        {
            get
            {
                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, OnboardProfileIndex, 0x4F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                shortEndpointStream.Close();
                longEndpointStream.Close();

                return response[5];
            }
        }

        public byte OnboardMode
        {
            get
            {
                var shortEndpointStream = ShortEndpoint.Open();
                var longEndpointStream = LongEndpoint.Open();

                // Can be set with function 0x10
                shortEndpointStream.Write(new byte[] { 0x10, 0xFF, OnboardProfileIndex, 0x2F, 0x00, 0x00, 0x00 });
                var response = longEndpointStream.Read();

                shortEndpointStream.Close();
                longEndpointStream.Close();

                return response[4];
            }
        }

        public Config ReadConfig()
        {
            var configBytes = new List<byte>();

            var shortEndpointStream = ShortEndpoint.Open();
            var longEndpointStream = LongEndpoint.Open();

            var readConfigCommand = new byte[] { 0x11, 0xFF, OnboardProfileIndex, 0x5F, 0x00, 0x01, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            for (int i = 0; i < 0x10; i++)
            {
                readConfigCommand[7] = (byte)(i * 16);

                longEndpointStream.Write(readConfigCommand);
                configBytes.AddRange(longEndpointStream.Read().Skip(4));
            }

            shortEndpointStream.Close();
            longEndpointStream.Close();

            return Config.FromBytes(configBytes, ButtonCount);
        }

        public void WriteConfig()
        {
            var configBytes = Config.ToBytes();
            var originalConfigBytes = OriginalConfig.ToBytes();

            // This is my implementation of OMM's "dirty" system. It will only write the config if changes have been made.
            if (configBytes.SequenceEqual(originalConfigBytes))
            {
                return;
            }

            var configBytesWithHeaders = new List<byte>() { 0x11, 0xFF, OnboardProfileIndex, 0x6F, 0x00, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            foreach (var item in configBytes.Chunk(16))
            {
                configBytesWithHeaders.AddRange(new byte[] { 0x11, 0xFF, OnboardProfileIndex, 0x7F });
                configBytesWithHeaders.AddRange(item);
            }

            var shortEndpointStream = ShortEndpoint.Open();
            var longEndpointStream = LongEndpoint.Open();

            foreach (var message in configBytesWithHeaders.Chunk(20))
            {
                longEndpointStream.Write(message);
            }

            shortEndpointStream.Write(new byte[] { 0x10, 0xFF, OnboardProfileIndex, 0x8F, 0x00, 0x00, 0x00 });

            shortEndpointStream.Close();
            longEndpointStream.Close();
        }
    }
}
