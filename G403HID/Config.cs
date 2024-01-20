using System.Text;

namespace G403HID
{
    public class Config
    {
        public enum ReportRate
        {
            RR1000 = 1,
            RR500 = 2,
            RR250 = 4,
            RR125 = 8
        }

        public enum RGBMode
        {
            Off = 0,
            Fixed = 1,
            Cycling = 3,
            Breathing = 10
        }

        public enum MouseButton : uint
        {
            LeftButton = 0x1000180,
            RightButton = 0x2000180,
            MiddleButton = 0x4000180,
            NavigateBackward = 0x8000180,
            NavigateForward = 0x10000180,
            NextTrack = 0xB50C0380,
            PreviousTrack = 0xB60C0380,
            PlayPause = 0xCD0C0380,
            VolumeMute = 0xE20C0380,
            VolumeUp = 0xE90C0380,
            VolumeDown = 0xEA0C0380,
            NoAction = 0xFFFF0090,
            WheelLeft = 0xFFFF0190,
            WheelRight = 0xFFFF0290,
            DPICycle = 0xFFFF0590,
            GShift = 0xFFFF0B90
        }

        public ReportRate DeviceReportRate;
        public byte ProfileDPIIndex;
        public byte DPIShiftIndex;
        public ushort[] DPIs = new ushort[5];

        public List<byte> Unknown1 = new();

        public List<ButtonMapping> MouseButtonMappings = new();

        public List<ButtonMapping> MouseButtonGShiftMappings = new();

        public string ProfileText = string.Empty;

        public RGBZone PalmRestRGB = new();

        public RGBZone WheelRGB = new();

        public List<byte> Unknown2 = new();

        public ushort Checksum;

        public static Config FromBytes(List<byte> bytes, byte buttonCount)
        {
            var config = new Config();

            using (var configReader = new BinaryReader(new MemoryStream(bytes.ToArray())))
            {
                config.DeviceReportRate = (ReportRate)configReader.ReadByte();
                config.ProfileDPIIndex = configReader.ReadByte();
                config.DPIShiftIndex = configReader.ReadByte();

                for (var i = 0; i < 5; i++)
                {
                    config.DPIs[i] = configReader.ReadUInt16();
                }

                config.Unknown1.AddRange(configReader.ReadBytes(19));

                for (int i = 0; i < buttonCount; i++)
                {
                    config.MouseButtonMappings.Add(ButtonMappingFactory.NewMapping(configReader.ReadBytes(4)));
                }
                config.MouseButtonMappings = config.MouseButtonMappings.Take(buttonCount).ToList();

                configReader.ReadBytes((16 - buttonCount) * 4);

                for (int i = 0; i < buttonCount; i++)
                {
                    config.MouseButtonGShiftMappings.Add(ButtonMappingFactory.NewMapping(configReader.ReadBytes(4)));
                }
                config.MouseButtonGShiftMappings = config.MouseButtonGShiftMappings.Take(buttonCount).ToList();

                configReader.ReadBytes((16 - buttonCount) * 4);

                config.ProfileText = Encoding.Unicode.GetString(configReader.ReadBytes(48));

                config.PalmRestRGB = RGBZone.FromBytes(configReader.ReadBytes(11).ToList());

                config.WheelRGB = RGBZone.FromBytes(configReader.ReadBytes(11).ToList());

                config.Unknown2.AddRange(configReader.ReadBytes(24));

                config.Checksum = BitConverter.ToUInt16(configReader.ReadBytes(2).Reverse().ToArray());
            }

            return config;
        }

        public List<byte> ToBytes()
        {
            var configBytes = new List<byte>
            {
                (byte)DeviceReportRate,
                ProfileDPIIndex,
                DPIShiftIndex
            };

            foreach (var item in DPIs)
            {
                configBytes.AddRange(BitConverter.GetBytes(item));
            }

            configBytes.AddRange(Unknown1);

            foreach (var item in MouseButtonMappings)
            {
                configBytes.AddRange(item.ToBytes());
            }
            for (int i = MouseButtonMappings.Count; i < 16; i++)
            {
                configBytes.AddRange(new SimpleMapping(SimpleMapping.MouseButton.NoButton).ToBytes());
            }

            foreach (var item in MouseButtonGShiftMappings)
            {
                configBytes.AddRange(item.ToBytes());
            }
            for (int i = MouseButtonGShiftMappings.Count; i < 16; i++)
            {
                configBytes.AddRange(new SimpleMapping(SimpleMapping.MouseButton.NoButton).ToBytes());
            }

            if (ProfileText.Length > 24)
            {
                ProfileText = ProfileText[..24];
            }
            while (Encoding.Unicode.GetBytes(ProfileText).Length != 48)
            {
                ProfileText += '\0';
            }
            configBytes.AddRange(Encoding.Unicode.GetBytes(ProfileText));

            configBytes.AddRange(PalmRestRGB.ToBytes());

            configBytes.AddRange(WheelRGB.ToBytes());

            configBytes.AddRange(Unknown2);

            configBytes.AddRange(BitConverter.GetBytes(NullFX.CRC.Crc16.ComputeChecksum(NullFX.CRC.Crc16Algorithm.CcittInitialValue0xFFFF, configBytes.ToArray())).Reverse());

            return configBytes;
        }
    }
}
