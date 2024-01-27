using System.Text;

namespace G403HID
{
    public class Profile
    {
        public enum ReportRate
        {
            RR1000 = 1,
            RR500 = 2,
            RR250 = 4,
            RR125 = 8
        }

        public ReportRate DeviceReportRate;
        public byte ProfileDPIIndex;
        public byte DPIShiftIndex;
        public ushort[] DPIs = new ushort[5];

        public List<byte> Unknown1 = new();

        public List<ButtonMapping> MouseButtonMappings = new();

        public List<ButtonMapping> MouseButtonGShiftMappings = new();

        public string ProfileText = string.Empty;

        public RGBZone RGBZone1 = new();

        public RGBZone RGBZone2 = new();

        public List<byte> Unknown2 = new();

        public ushort Checksum;

        public static Profile FromBytes(List<byte> bytes, byte buttonCount)
        {
            var profile = new Profile();

            using (var profileReader = new BinaryReader(new MemoryStream(bytes.ToArray())))
            {
                profile.DeviceReportRate = (ReportRate)profileReader.ReadByte();
                profile.ProfileDPIIndex = profileReader.ReadByte();
                profile.DPIShiftIndex = profileReader.ReadByte();

                for (var i = 0; i < 5; i++)
                {
                    profile.DPIs[i] = profileReader.ReadUInt16();
                }

                profile.Unknown1.AddRange(profileReader.ReadBytes(19));

                for (int i = 0; i < buttonCount; i++)
                {
                    profile.MouseButtonMappings.Add(ButtonMappingFactory.NewMapping(profileReader.ReadBytes(4)));
                }
                profile.MouseButtonMappings = profile.MouseButtonMappings.Take(buttonCount).ToList();

                profileReader.ReadBytes((16 - buttonCount) * 4);

                for (int i = 0; i < buttonCount; i++)
                {
                    profile.MouseButtonGShiftMappings.Add(ButtonMappingFactory.NewMapping(profileReader.ReadBytes(4)));
                }
                profile.MouseButtonGShiftMappings = profile.MouseButtonGShiftMappings.Take(buttonCount).ToList();

                profileReader.ReadBytes((16 - buttonCount) * 4);

                profile.ProfileText = Encoding.Unicode.GetString(profileReader.ReadBytes(48));

                profile.RGBZone1 = RGBZone.FromBytes(profileReader.ReadBytes(11).ToList());

                profile.RGBZone2 = RGBZone.FromBytes(profileReader.ReadBytes(11).ToList());

                profile.Unknown2.AddRange(profileReader.ReadBytes(24));

                profile.Checksum = BitConverter.ToUInt16(profileReader.ReadBytes(2).Reverse().ToArray());
            }

            return profile;
        }

        public List<byte> ToBytes()
        {
            var profileBytes = new List<byte>
            {
                (byte)DeviceReportRate,
                ProfileDPIIndex,
                DPIShiftIndex
            };

            foreach (var item in DPIs)
            {
                profileBytes.AddRange(BitConverter.GetBytes(item));
            }

            profileBytes.AddRange(Unknown1);

            foreach (var item in MouseButtonMappings)
            {
                profileBytes.AddRange(item.ToBytes());
            }
            for (int i = MouseButtonMappings.Count; i < 16; i++)
            {
                profileBytes.AddRange(new SimpleMapping(SimpleMapping.MouseButton.NoButton).ToBytes());
            }

            foreach (var item in MouseButtonGShiftMappings)
            {
                profileBytes.AddRange(item.ToBytes());
            }
            for (int i = MouseButtonGShiftMappings.Count; i < 16; i++)
            {
                profileBytes.AddRange(new SimpleMapping(SimpleMapping.MouseButton.NoButton).ToBytes());
            }

            if (ProfileText.Length > 24)
            {
                ProfileText = ProfileText[..24];
            }
            while (Encoding.Unicode.GetBytes(ProfileText).Length != 48)
            {
                ProfileText += '\0';
            }
            profileBytes.AddRange(Encoding.Unicode.GetBytes(ProfileText));

            profileBytes.AddRange(RGBZone1.ToBytes());

            profileBytes.AddRange(RGBZone2.ToBytes());

            profileBytes.AddRange(Unknown2);

            profileBytes.AddRange(BitConverter.GetBytes(Checksum).Reverse());

            return profileBytes;
        }
    }
}
